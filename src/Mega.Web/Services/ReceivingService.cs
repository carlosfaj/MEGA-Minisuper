using System.Globalization;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed record SupplierChange(long? Id, long Version, string Name, string? Identification, string? Phone, string? Address, string? Contact, bool Active);
public sealed record ReceivingLine(long PresentationId, string? Received, string? Accepted, string? BaseCost, string? Expiry, string? Lot, string? Note);
public sealed record ReceivingChange(long? Id, long Version, string Key, string Reference, long? SupplierId, string? Document, string? DocumentDate, string? Notes, List<ReceivingLine> Lines);
public sealed record ReceiptReview(long ReceiptId, long Version, long CreatorId, string Reference, long? SupplierId, long? SupplierVersion, string? Supplier, string? Document, string? DocumentDate, string? Notes, string Currency, List<ReceiptReviewLine> Lines);
public sealed record ReceiptReviewLine(long DetailId, long Number, long PresentationId, string Presentation, long PresentationVersion, long ProductId, string Product, long ProductVersion, string Unit, long Scale, long ExpirationControl, long Factor, long FactorScale, long? Received, long? Accepted, long? BaseQuantity, string CostState, long? BaseCost, long? CostScale, string? Expiry, string? Lot, string? Note);

public sealed class ReceivingService(MegaDbContext db, AccessControl access, Operations operations)
{
    private Task<Usuario> InventoryAsync(long id) => access.RequireAsync(id, "Inventario", "Administrador", "Propietario");
    public async Task<long> SaveSupplierAsync(long userId, SupplierChange input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.ManagerAsync(userId);
        var name = AccessControl.Text(input.Name, "Proveedor"); var identification = AccessControl.Optional(input.Identification, "Identificación", 100);
        if (identification is not null && await db.Set<Proveedor>().AnyAsync(x => x.Id != input.Id && x.Identificacion == identification)) throw new BusinessException("Esa identificación pertenece a otro proveedor, incluso si está desactivado.");
        var p = input.Id is null ? new Proveedor { Version = 1 } : await db.Set<Proveedor>().SingleOrDefaultAsync(x => x.Id == input.Id) ?? throw new BusinessException("Proveedor no encontrado.");
        if (input.Id is not null) AccessControl.Version(p.Version, input.Version);
        var before = input.Id is null ? null : SnapshotSupplier(p);
        p.Nombre = name; p.Identificacion = identification; p.Telefono = AccessControl.Optional(input.Phone, "Teléfono", 100);
        p.Direccion = AccessControl.Optional(input.Address, "Dirección", 500); p.Contacto = AccessControl.Optional(input.Contact, "Contacto", 200); p.Activo = input.Active ? 1 : 0;
        if (input.Id is null) db.Add(p); else p.Version++;
        await db.SaveChangesAsync(); operations.Audit(userId, input.Id is null ? "Crear proveedor" : "Actualizar proveedor", "Proveedor", p.Id.ToString(), Guid.NewGuid().ToString(), new { antes = before, despues = SnapshotSupplier(p) });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return p.Id;
    }
    private static object SnapshotSupplier(Proveedor p) => new { p.Nombre, p.Identificacion, p.Telefono, p.Direccion, p.Contacto, p.Activo, p.Version };

    public async Task<long> SaveAsync(long userId, ReceivingChange input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await InventoryAsync(userId);
        if (!Guid.TryParseExact(input.Key, "D", out _)) throw new BusinessException("La preparación no tiene una clave válida. Abre una recepción nueva.");
        var reference = AccessControl.Text(input.Reference, "Referencia", 100); var document = AccessControl.Optional(input.Document, "Documento", 100);
        if (await db.Set<Recepcion>().AnyAsync(x => x.Id != input.Id && (x.Referencia == reference || x.ClaveSolicitud == input.Key))) throw new BusinessException("Esta referencia o preparación ya está registrada. Consúltala en Recepciones.");
        if (document is not null && input.SupplierId is not null && await db.Set<Recepcion>().AnyAsync(x => x.Id != input.Id && x.ProveedorId == input.SupplierId && x.Documento == document)) throw new BusinessException("Ese documento del proveedor ya está registrado.");
        if (input.SupplierId is not null && !await db.Set<Proveedor>().AnyAsync(x => x.Id == input.SupplierId && x.Activo == 1)) throw new BusinessException("Selecciona un proveedor activo.");
        if (input.Lines is null || input.Lines.Count > 100) throw new BusinessException("La recepción admite hasta 100 líneas.");
        var receipt = input.Id is null ? new Recepcion { CreadoPor = userId, CreadoEn = Formats.Now(), Version = 1, ClaveSolicitud = input.Key, Moneda = "NIO" } :
            await db.Set<Recepcion>().SingleOrDefaultAsync(x => x.Id == input.Id) ?? throw new BusinessException("Recepción no encontrada.");
        object? before = null;
        if (input.Id is not null)
        {
            if (receipt.CreadoPor != userId) throw new BusinessException("Solo quien preparó la recepción puede modificarla o enviarla.");
            AccessControl.Version(receipt.Version, input.Version);
            if (receipt.Estado == "Confirmada") throw new BusinessException("La recepción confirmada conserva su contenido. No se puede editar.");
            if (receipt.ClaveSolicitud != input.Key) throw new BusinessException("La clave de preparación cambió.");
            before = await ReviewAsync(receipt.Id);
        }
        var details = new List<RecepcionDetalle>(); var number = 0;
        foreach (var line in input.Lines)
        {
            var presentation = await db.Set<Presentacion>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == line.PresentationId && x.Activo == 1);
            var product = presentation is null ? null : await db.Set<Producto>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == presentation.ProductoId && x.Activo == 1);
            if (presentation is null || product is null) throw new BusinessException("Cada línea requiere una presentación y producto activos.");
            var received = Quantity(line.Received, product.EscalaCantidad); var accepted = Quantity(line.Accepted, product.EscalaCantidad);
            if (received is not null && accepted > received) throw new BusinessException("La cantidad aceptada no puede superar la recibida.");
            long? baseQuantity = null;
            try { if (accepted is long qty) baseQuantity = Formats.Scaled(qty / Power(product.EscalaCantidad) * presentation.FactorEscalado / Power(presentation.EscalaFactor), (int)product.EscalaCantidad); }
            catch (OverflowException) { throw new BusinessException("La equivalencia supera la capacidad de registro."); }
            var cost = Quantity(line.BaseCost, 6);
            details.Add(new RecepcionDetalle { NumeroLinea = ++number, PresentacionId = presentation.Id, CantidadRecibidaEscalada = received, CantidadAceptadaEscalada = accepted,
                EscalaCantidad = product.EscalaCantidad, FactorHistoricoEscalado = presentation.FactorEscalado, EscalaFactor = presentation.EscalaFactor, CantidadBaseEscalada = baseQuantity,
                CostoUnitEscalado = cost, EscalaCosto = cost is null ? null : 6, EstadoCosto = cost is null ? "desconocido" : "conocido", Vencimiento = Date(line.Expiry, "Vencimiento"),
                LoteReferencia = AccessControl.Optional(line.Lot, "Lote", 100), Observacion = AccessControl.Optional(line.Note, "Observación de línea", 1000) });
        }
        receipt.Referencia = reference; receipt.ProveedorId = input.SupplierId; receipt.Documento = document; receipt.FechaDocumento = Date(input.DocumentDate, "Fecha del documento");
        receipt.Observaciones = AccessControl.Optional(input.Notes, "Observaciones", 2000); receipt.ModificadoEn = Formats.Now(); receipt.Estado = "Pendiente";
        if (input.Id is null) { db.Add(receipt); await db.SaveChangesAsync(); }
        else
        {
            receipt.Version++;
            foreach (var auth in await db.Set<Autorizacion>().Where(x => x.RecepcionId == receipt.Id && x.InvalidadaEn == null && x.Decision != "rechazada").ToListAsync())
            { auth.InvalidadaEn = Formats.Now(); auth.MotivoInvalidacion = "Recepción editada: requiere revisión de la nueva versión."; }
            db.RemoveRange(await db.Set<RecepcionDetalle>().Where(x => x.RecepcionId == receipt.Id).ToListAsync()); await db.SaveChangesAsync();
        }
        foreach (var d in details) { d.RecepcionId = receipt.Id; db.Add(d); }
        await db.SaveChangesAsync(); operations.Audit(userId, input.Id is null ? "Preparar recepción" : "Editar recepción", "Recepcion", receipt.Id.ToString(), Guid.NewGuid().ToString(), new { antes = before, despues = await ReviewAsync(receipt.Id) });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return receipt.Id;
    }
    public async Task<ReceiptReview> ReviewAsync(long id)
    {
        var r = await db.Set<Recepcion>().AsNoTracking().SingleAsync(x => x.Id == id);
        var supplier = await db.Set<Proveedor>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == r.ProveedorId);
        var lines = new List<ReceiptReviewLine>();
        foreach (var d in await db.Set<RecepcionDetalle>().AsNoTracking().Where(x => x.RecepcionId == id).OrderBy(x => x.NumeroLinea).ToListAsync())
        {
            var p = await db.Set<Presentacion>().AsNoTracking().SingleAsync(x => x.Id == d.PresentacionId);
            var product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == p.ProductoId);
            lines.Add(new(d.Id, d.NumeroLinea, p.Id, p.Nombre, p.Version, product.Id, product.Nombre, product.Version, product.UnidadBase, d.EscalaCantidad, product.ControlVencimiento,
                d.FactorHistoricoEscalado, d.EscalaFactor, d.CantidadRecibidaEscalada, d.CantidadAceptadaEscalada, d.CantidadBaseEscalada, d.EstadoCosto, d.CostoUnitEscalado, d.EscalaCosto, d.Vencimiento, d.LoteReferencia, d.Observacion));
        }
        return new(r.Id, r.Version, r.CreadoPor, r.Referencia, r.ProveedorId, supplier?.Version, supplier?.Nombre, r.Documento, r.FechaDocumento, r.Observaciones, r.Moneda, lines);
    }
    private async Task ReadyAsync(ReceiptReview review)
    {
        if (review.Currency != "NIO" || review.SupplierId is null || !await db.Set<Proveedor>().AnyAsync(x => x.Id == review.SupplierId && x.Activo == 1)) throw new BusinessException("La recepción requiere un proveedor activo y moneda NIO.");
        if (review.Document is null || review.DocumentDate is null) throw new BusinessException("Completa el documento del proveedor y su fecha antes de enviar.");
        if (review.Lines.Count == 0 || !review.Lines.Any(x => x.BaseQuantity > 0)) throw new BusinessException("Registra al menos una cantidad aceptada mayor que cero.");
        var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-6)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        foreach (var d in review.Lines)
        {
            var p = await db.Set<Presentacion>().AsNoTracking().SingleAsync(x => x.Id == d.PresentationId);
            var product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == d.ProductId);
            if (p.Activo != 1 || product.Activo != 1 || product.EscalaCantidad != d.Scale || p.FactorEscalado != d.Factor || p.EscalaFactor != d.FactorScale) throw new BusinessException("El catálogo cambió. Edita y vuelve a revisar la recepción.");
            if (d.Received is null or <= 0 || d.Accepted is null || d.BaseQuantity is null) throw new BusinessException("Completa cantidades recibidas y aceptadas en todas las líneas.");
            if (d.Accepted > 0 && (d.CostState != "conocido" || d.BaseCost is null)) throw new BusinessException("La recepción ordinaria exige costo conocido por unidad base en lo aceptado. Vacío no significa cero.");
            if (d.Accepted > 0 && d.ExpirationControl == 1 && (d.Expiry is null || d.Lot is null)) throw new BusinessException("Este producto requiere lote y vencimiento en lo aceptado.");
            if (d.Accepted > 0 && d.Expiry is not null && string.CompareOrdinal(d.Expiry, today) < 0) throw new BusinessException("No se puede aceptar mercadería vencida. Conserva lo rechazado con cantidad aceptada cero.");
            if (d.Accepted < d.Received && string.IsNullOrWhiteSpace(d.Note)) throw new BusinessException("Explica por qué la cantidad aceptada es menor que la recibida.");
        }
    }
    public async Task<long> SubmitAsync(long userId, long receiptId, long version)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await InventoryAsync(userId);
        var r = await db.Set<Recepcion>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == receiptId) ?? throw new BusinessException("Recepción no encontrada.");
        if (r.CreadoPor != userId || r.Estado != "Pendiente") throw new BusinessException("Solo quien preparó una recepción pendiente puede enviarla.");
        AccessControl.Version(r.Version, version); var review = await ReviewAsync(receiptId); await ReadyAsync(review); var snapshot = Formats.Json(review); var hash = Formats.Hash(snapshot);
        var previous = await db.Set<Autorizacion>().AsNoTracking().SingleOrDefaultAsync(x => x.RecepcionId == receiptId && x.ObjetoVersion == version && x.InvalidadaEn == null && x.Decision != "rechazada");
        if (previous is not null)
        {
            if (previous.ContenidoHash != hash) throw new BusinessException("El contenido relacionado cambió. Guarda una nueva versión antes de enviar.");
            await tx.CommitAsync(); return previous.Id;
        }
        var auth = new Autorizacion { RecepcionId = receiptId, SolicitanteId = userId, FechaSolicitud = Formats.Now(), Decision = "pendiente", Modalidad = "segunda_cuenta", ObjetoVersion = version, ContenidoSnapshot = snapshot, ContenidoHash = hash };
        db.Add(auth); await db.SaveChangesAsync(); operations.Audit(userId, "Enviar recepción a revisión", "Recepcion", receiptId.ToString(), Guid.NewGuid().ToString(), new { autorizacionId = auth.Id, version, hash });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return auth.Id;
    }
    public async Task<long?> DecideAsync(long userId, long authId, long expectedVersion, string expectedHash, bool approve, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.ManagerAsync(userId);
        var auth = await db.Set<Autorizacion>().SingleOrDefaultAsync(x => x.Id == authId && x.RecepcionId != null) ?? throw new BusinessException("La revisión no existe.");
        if (auth.SolicitanteId == userId) throw new BusinessException("Debe revisar y decidir otra cuenta de gestión. No se permite autoaprobar.");
        if (auth.ObjetoVersion != expectedVersion || auth.ContenidoHash != expectedHash || Formats.Hash(auth.ContenidoSnapshot) != expectedHash || auth.InvalidadaEn is not null)
            throw new BusinessException("La revisión ya no corresponde a esta versión. Abre la revisión vigente.");
        var r = await db.Set<Recepcion>().SingleAsync(x => x.Id == auth.RecepcionId);
        if (approve && r.Estado == "Confirmada" && auth.Decision == "autorizada" && auth.AutorizadorId == userId && r.Version == expectedVersion)
        { await tx.CommitAsync(); return r.OperacionId; }
        if (auth.Decision != "pendiente" || r.Estado != "Pendiente") throw new BusinessException("La recepción o su revisión ya fueron resueltas.");
        var review = await ReviewAsync(r.Id);
        if (review.Version != expectedVersion || Formats.Hash(Formats.Json(review)) != expectedHash) throw new BusinessException("La recepción o el catálogo cambiaron después de abrir la revisión. Exige una nueva versión y revisión.");
        reason = AccessControl.Text(reason, "Motivo de la decisión", 1000);
        if (approve) await ReadyAsync(review);
        if (approve)
        {
            try
            {
                foreach (var group in review.Lines.GroupBy(x => x.ProductId))
                {
                    var current = await db.Set<MovimientoInventario>().Where(x => x.ProductoId == group.Key).SumAsync(x => (long?)x.CantidadEscalada) ?? 0;
                    foreach (var line in group) current = checked(current + (line.BaseQuantity ?? 0));
                }
            }
            catch (OverflowException) { throw new BusinessException("La entrada supera la capacidad de existencias. Revisa cantidades y equivalencias."); }
        }
        auth.AutorizadorId = userId; auth.FechaDecision = Formats.Now(); auth.Decision = approve ? "autorizada" : "rechazada"; auth.Motivo = reason;
        if (!approve)
        {
            r.Estado = "Rechazada"; r.ModificadoEn = Formats.Now();
            operations.Audit(userId, "Rechazar recepción", "Recepcion", r.Id.ToString(), Guid.NewGuid().ToString(), new { autorizacionId = auth.Id, version = r.Version, hash = expectedHash, motivo = reason }, authorizationId: auth.Id);
            await db.SaveChangesAsync(); await tx.CommitAsync(); return null;
        }
        // Decisión, procedencia, lotes y entrada se confirman juntos; ningún estado intermedio queda publicado.
        var now = Formats.Now(); var operation = new OperacionInventario { Tipo = "entrada_recepcion", Fecha = now, ConfirmadoEn = now, SecuenciaConfirmacion = checked((await db.Set<OperacionInventario>().MaxAsync(x => (long?)x.SecuenciaConfirmacion) ?? 0) + 1), CreadoPor = r.CreadoPor, EjecutadoPor = userId, AutorizacionId = auth.Id, ClaveOperacion = "recepcion-" + r.ClaveSolicitud, Motivo = reason };
        db.Add(operation); await db.SaveChangesAsync(); r.OperacionId = operation.Id; r.Estado = "Confirmada"; r.ModificadoEn = now; await db.SaveChangesAsync();
        var sequence = 0;
        foreach (var d in review.Lines.Where(x => x.BaseQuantity > 0))
        {
            long? lotId = null;
            if (d.Expiry is not null || d.Lot is not null)
            { var lot = new LoteExistencia { ProductoId = d.ProductId, RecepcionDetalleId = d.DetailId, Vencimiento = d.Expiry, Referencia = d.Lot, CreadoEn = now }; db.Add(lot); await db.SaveChangesAsync(); lotId = lot.Id; }
            db.Add(new MovimientoInventario { OperacionId = operation.Id, ProductoId = d.ProductId, LoteId = lotId, Secuencia = ++sequence, RecepcionDetalleId = d.DetailId, CantidadEscalada = d.BaseQuantity!.Value, Escala = d.Scale, EstadoCosto = d.CostState, CostoUnitEscalado = d.BaseCost, EscalaCosto = d.CostScale });
        }
        await db.SaveChangesAsync();
        operations.Audit(userId, "Confirmar recepción autorizada", "Recepcion", r.Id.ToString(), operation.ClaveOperacion, new { autorizacionId = auth.Id, version = r.Version, hash = expectedHash, operacionId = operation.Id, motivo = reason }, authorizationId: auth.Id);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return operation.Id;
    }
    private static long? Quantity(string? value, long scale) => string.IsNullOrWhiteSpace(value) ? null : Formats.Scaled(Formats.Parse(value), (int)scale);
    public static decimal Power(long scale) => (decimal)Math.Pow(10, scale);
    private static string? Date(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) throw new BusinessException(field + ": introduce una fecha válida.");
        return value;
    }
}
