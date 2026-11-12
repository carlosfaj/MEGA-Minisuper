using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed record InventoryLine(long ProductId, string Quantity, string? BaseCost, string? Cause, long? ReturnSupplierId, string? Evidence);
public sealed record InventoryChange(long? Id, long Version, string Key, string Reference, string Type, long? CountId, string Reason, string Evidence, List<InventoryLine> Lines);
public sealed record InventoryReview(long RequestId, long Version, long RequesterId, string Reference, string Type, long? CountId, long? CountVersion, string? Reason, string? Evidence, List<InventoryReviewLine> Lines);
public sealed record InventoryReviewLine(long DetailId, long Number, long ProductId, string Product, long ProductVersion, string Unit, long Scale, long Quantity, long Balance, string CostState, long? BaseCost, long? CostScale, string? Cause, long? ReturnSupplierId, long? SupplierVersion, string? Supplier, string? Evidence);

public sealed class InventoryRequestService(MegaDbContext db, AccessControl access, Operations operations)
{
    public static readonly string[] Causes = ["devolucion_proveedor", "robo", "dano", "vencimiento", "perdida", "consumo_interno"];
    private Task<Usuario> RequireAsync(long id) => access.RequireAsync(id, "Inventario", "Administrador", "Propietario");
    public async Task<long> SaveAsync(long userId, InventoryChange input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await RequireAsync(userId);
        if (!Guid.TryParseExact(input.Key, "D", out _) || input.Type is not ("carga" or "ajuste" or "salida_justificada")) throw new BusinessException("La preparación o su tipo no son válidos.");
        var reference = AccessControl.Text(input.Reference, "Referencia", 100); var reason = AccessControl.Text(input.Reason, "Motivo", 1000); var evidence = AccessControl.Text(input.Evidence, "Referencia de evidencia", 1000);
        if (await db.Set<SolicitudInventario>().AnyAsync(x => x.Id != input.Id && (x.Referencia == reference || x.ClaveSolicitud == input.Key))) throw new BusinessException("La referencia o preparación ya está registrada.");
        var request = input.Id is null ? new SolicitudInventario { SolicitadoPor = userId, CreadoEn = Formats.Now(), Version = 1, ClaveSolicitud = input.Key, Tipo = input.Type, ConteoId = input.CountId } :
            await db.Set<SolicitudInventario>().SingleOrDefaultAsync(x => x.Id == input.Id) ?? throw new BusinessException("Solicitud no encontrada.");
        object? before = null;
        if (input.Id is not null)
        {
            if (request.SolicitadoPor != userId || request.Estado is "Autorizada" or "Cancelada") throw new BusinessException("Solo quien preparó una solicitud sin aplicar puede editarla.");
            AccessControl.Version(request.Version, input.Version);
            if (request.ClaveSolicitud != input.Key || request.Tipo != input.Type || request.ConteoId != input.CountId) throw new BusinessException("El tipo, conteo y clave de una preparación se conservan.");
            before = await ReviewAsync(request.Id); request.Version++;
        }
        var details = new List<SolicitudInventarioDetalle>();
        if (input.Type == "ajuste")
        {
            if (input.Lines.Count != 0) throw new BusinessException("Las cantidades del ajuste se obtienen exclusivamente del conteo finalizado.");
            var count = await CountReadyAsync(input.CountId);
            if (await db.Set<SolicitudInventario>().AnyAsync(x => x.Id != input.Id && x.ConteoId == count.Id && x.Estado != "Cancelada" && x.Estado != "Rechazada")) throw new BusinessException("Ese conteo ya tiene una solicitud vigente o aplicada.");
            foreach (var d in await db.Set<ConteoDetalle>().AsNoTracking().Where(x => x.ConteoId == count.Id).OrderBy(x => x.Id).ToListAsync())
            {
                var quantity = checked(d.CantidadFisicaEscalada - d.SaldoReferenciaEscalado); if (quantity == 0) continue;
                if (d.LoteId is not null) throw new BusinessException("El ajuste por lotes requiere la integración de valoración pendiente.");
                details.Add(new() { ProductoId = d.ProductoId, CantidadEscalada = quantity, Escala = d.Escala, EstadoCosto = "desconocido", EvidenciaReferencia = evidence });
            }
        }
        else
        {
            if (input.CountId is not null || input.Lines.Count is 0 or >100) throw new BusinessException("La carga o salida requiere entre 1 y 100 líneas y no utiliza un conteo.");
            foreach (var line in input.Lines)
            {
                var p = await db.Set<Producto>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == line.ProductId) ?? throw new BusinessException("Selecciona un producto válido.");
                var quantity = Formats.Scaled(Formats.Parse(line.Quantity), (int)p.EscalaCantidad); if (quantity <= 0) throw new BusinessException("La cantidad debe ser mayor que cero.");
                long? cost = string.IsNullOrWhiteSpace(line.BaseCost) ? null : Formats.Scaled(Formats.Parse(line.BaseCost), 6);
                if (input.Type == "salida_justificada" && cost is not null) throw new BusinessException("La salida conserva el costo de origen; no admite asignar un costo manual.");
                if (input.Type == "salida_justificada" && !Causes.Contains(line.Cause ?? "")) throw new BusinessException("Selecciona una causa válida para la salida.");
                details.Add(new() { ProductoId = p.Id, CantidadEscalada = input.Type == "carga" ? quantity : -quantity, Escala = p.EscalaCantidad, EstadoCosto = cost is null ? "desconocido" : "conocido", CostoUnitEscalado = cost, EscalaCosto = cost is null ? null : 6,
                    Causa = input.Type == "salida_justificada" ? line.Cause : null, ProveedorDevolucionId = line.Cause == "devolucion_proveedor" ? line.ReturnSupplierId : null,
                    EvidenciaReferencia = AccessControl.Text(line.Evidence ?? evidence, "Evidencia de línea", 1000) });
            }
        }
        if (details.Count == 0 || details.Count > 100) throw new BusinessException("La solicitud no tiene diferencias o líneas aplicables.");
        if (details.GroupBy(x => x.ProductoId).Any(g => g.Count() > 1)) throw new BusinessException("Registra una sola línea por producto en esta solicitud.");
        foreach (var d in details) await SupportedAsync(d.ProductoId, d.Causa, d.ProveedorDevolucionId);
        request.Referencia = reference; request.Motivo = reason; request.EvidenciaReferencia = evidence; request.Estado = "En_preparacion"; request.EnviadoEn = null; request.ResueltoEn = null;
        if (input.Id is null) { db.Add(request); await db.SaveChangesAsync(); }
        else
        {
            foreach (var auth in await db.Set<Autorizacion>().Where(x => x.SolicitudInventarioId == request.Id && x.InvalidadaEn == null && x.Decision != "rechazada").ToListAsync()) { auth.InvalidadaEn = Formats.Now(); auth.MotivoInvalidacion = "Solicitud editada: requiere revisión nueva."; }
            db.RemoveRange(await db.Set<SolicitudInventarioDetalle>().Where(x => x.SolicitudId == request.Id).ToListAsync()); await db.SaveChangesAsync();
        }
        var number = 0; foreach (var d in details) { d.SolicitudId = request.Id; d.NumeroLinea = ++number; db.Add(d); }
        await db.SaveChangesAsync(); operations.Audit(userId, "Guardar solicitud de inventario", "SolicitudInventario", request.Id.ToString(), Guid.NewGuid().ToString(), new { antes = before, despues = await ReviewAsync(request.Id), alteraExistencias = false });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return request.Id;
    }
    private async Task SupportedAsync(long productId, string? cause, long? returnSupplier)
    {
        var p = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == productId);
        if (p.ControlVencimiento == 1 || await db.Set<LoteExistencia>().AnyAsync(x => x.ProductoId == productId)) throw new BusinessException("Esta operación requiere asignación por lotes y valoración, pendientes de integrar.");
        if (await db.Set<MovimientoInventario>().AnyAsync(x => x.ProductoId == productId && x.EstadoCosto == "conocido")) throw new BusinessException("Este producto conserva costo conocido. Requiere valoración antes de esta regularización; no se convertirá en desconocido.");
        if (cause == "devolucion_proveedor" && (returnSupplier is null || !await db.Set<Proveedor>().AnyAsync(x => x.Id == returnSupplier && x.Activo == 1))) throw new BusinessException("La devolución requiere un proveedor activo.");
    }
    private async Task<Conteo> CountReadyAsync(long? id)
    {
        var count = await db.Set<Conteo>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Estado == "Finalizado") ?? throw new BusinessException("El ajuste requiere un conteo finalizado.");
        var sequence = count.OperacionReferenciaId is long op ? await db.Set<OperacionInventario>().Where(x => x.Id == op).Select(x => x.SecuenciaConfirmacion).SingleAsync() : 0;
        foreach (var d in await db.Set<ConteoDetalle>().AsNoTracking().Where(x => x.ConteoId == id).ToListAsync())
            if (await db.Set<MovimientoInventario>().AnyAsync(x => x.ProductoId == d.ProductoId && (d.LoteId == null || x.LoteId == d.LoteId) && db.Set<OperacionInventario>().Any(o => o.Id == x.OperacionId && o.SecuenciaConfirmacion > sequence)))
                throw new BusinessException("Hubo movimientos después del conteo. Registra y finaliza un conteo nuevo antes de ajustar.");
        return count;
    }
    public async Task<InventoryReview> ReviewAsync(long id)
    {
        var r = await db.Set<SolicitudInventario>().AsNoTracking().SingleAsync(x => x.Id == id); var lines = new List<InventoryReviewLine>();
        foreach (var d in await db.Set<SolicitudInventarioDetalle>().AsNoTracking().Where(x => x.SolicitudId == id).OrderBy(x => x.NumeroLinea).ToListAsync())
        {
            var p = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == d.ProductoId); var supplier = await db.Set<Proveedor>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == d.ProveedorDevolucionId);
            var balance = await db.Set<MovimientoInventario>().Where(x => x.ProductoId == p.Id).SumAsync(x => (long?)x.CantidadEscalada) ?? 0;
            lines.Add(new(d.Id, d.NumeroLinea, p.Id, p.Nombre, p.Version, p.UnidadBase, d.Escala, d.CantidadEscalada, balance, d.EstadoCosto, d.CostoUnitEscalado, d.EscalaCosto, d.Causa, d.ProveedorDevolucionId, supplier?.Version, supplier?.Nombre, d.EvidenciaReferencia));
        }
        var version = r.ConteoId is long c ? await db.Set<Conteo>().Where(x => x.Id == c).Select(x => (long?)x.Version).SingleAsync() : null;
        return new(r.Id, r.Version, r.SolicitadoPor, r.Referencia, r.Tipo, r.ConteoId, version, r.Motivo, r.EvidenciaReferencia, lines);
    }
    private async Task ReadyAsync(InventoryReview review)
    {
        if (review.Lines.Count is 0 or >100) throw new BusinessException("La solicitud requiere líneas aplicables.");
        if (review.Type == "ajuste") await CountReadyAsync(review.CountId);
        foreach (var d in review.Lines)
        {
            await SupportedAsync(d.ProductId, d.Cause, d.ReturnSupplierId);
            var product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == d.ProductId);
            if (product.EscalaCantidad != d.Scale || d.Quantity == 0 || review.Type == "carga" && d.Quantity < 0 || review.Type == "salida_justificada" && (d.Quantity > 0 || !Causes.Contains(d.Cause ?? ""))) throw new BusinessException("La cantidad, causa o unidad no corresponde al tipo de solicitud.");
            long next; try { next = checked(d.Balance + d.Quantity); } catch (OverflowException) { throw new BusinessException("La cantidad supera la capacidad de existencias."); }
            if (next < 0) throw new BusinessException("La solicitud dejaría existencias negativas. Revisa disponibilidad y evidencia física.");
        }
    }
    public async Task<long> SubmitAsync(long userId, long id, long version)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await RequireAsync(userId);
        var r = await db.Set<SolicitudInventario>().SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Solicitud no encontrada.");
        if (r.SolicitadoPor != userId || r.Estado is not ("En_preparacion" or "Pendiente")) throw new BusinessException("Solo quien preparó una solicitud vigente puede enviarla.");
        AccessControl.Version(r.Version, version); var review = await ReviewAsync(id); await ReadyAsync(review); var snapshot = Formats.Json(review); var hash = Formats.Hash(snapshot);
        var old = await db.Set<Autorizacion>().AsNoTracking().SingleOrDefaultAsync(x => x.SolicitudInventarioId == id && x.ObjetoVersion == version && x.InvalidadaEn == null && x.Decision != "rechazada");
        if (old is not null) { if (old.ContenidoHash != hash) throw new BusinessException("El inventario cambió. Guarda una versión nueva y envía otra revisión."); await tx.CommitAsync(); return old.Id; }
        r.Estado = "Pendiente"; r.EnviadoEn = Formats.Now(); var auth = new Autorizacion { SolicitudInventarioId = id, SolicitanteId = userId, FechaSolicitud = r.EnviadoEn, Decision = "pendiente", Modalidad = "segunda_cuenta", ObjetoVersion = version, ContenidoSnapshot = snapshot, ContenidoHash = hash };
        db.Add(auth); await db.SaveChangesAsync(); operations.Audit(userId, "Enviar solicitud de inventario", "SolicitudInventario", id.ToString(), Guid.NewGuid().ToString(), new { version, hash }, authorizationId: auth.Id);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return auth.Id;
    }
    public async Task<long?> DecideAsync(long userId, long authId, long version, string hash, bool approve, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.ManagerAsync(userId);
        var auth = await db.Set<Autorizacion>().SingleOrDefaultAsync(x => x.Id == authId && x.SolicitudInventarioId != null) ?? throw new BusinessException("Revisión no encontrada.");
        if (auth.SolicitanteId == userId) throw new BusinessException("Debe decidir otra cuenta de gestión. No se permite autoaprobar.");
        if (auth.ObjetoVersion != version || auth.ContenidoHash != hash || Formats.Hash(auth.ContenidoSnapshot) != hash || auth.InvalidadaEn is not null) throw new BusinessException("La revisión quedó obsoleta. Abre la versión vigente.");
        var r = await db.Set<SolicitudInventario>().SingleAsync(x => x.Id == auth.SolicitudInventarioId);
        if (approve && r.Estado == "Autorizada" && r.Version == version && auth.Decision == "autorizada" && auth.AutorizadorId == userId) { await tx.CommitAsync(); return r.OperacionId; }
        if (r.Estado != "Pendiente" || auth.Decision != "pendiente") throw new BusinessException("La solicitud o revisión ya están resueltas.");
        var review = await ReviewAsync(r.Id);
        if (r.Version != version || Formats.Hash(Formats.Json(review)) != hash) throw new BusinessException("El contenido o las existencias cambiaron desde la revisión. Guarda una versión nueva antes de decidir.");
        reason = AccessControl.Text(reason, "Motivo de la decisión", 1000); if (approve) await ReadyAsync(review);
        var now = Formats.Now(); auth.Decision = approve ? "autorizada" : "rechazada"; auth.AutorizadorId = userId; auth.FechaDecision = now; auth.Motivo = reason; r.ResueltoEn = now;
        if (!approve) r.Estado = "Rechazada";
        else
        {
            var op = new OperacionInventario { Tipo = r.Tipo, Fecha = now, ConfirmadoEn = now, SecuenciaConfirmacion = checked((await db.Set<OperacionInventario>().MaxAsync(x => (long?)x.SecuenciaConfirmacion) ?? 0) + 1), CreadoPor = r.SolicitadoPor, EjecutadoPor = userId, AutorizacionId = auth.Id, ClaveOperacion = "inventario-" + r.ClaveSolicitud, Motivo = r.Motivo };
            db.Add(op); await db.SaveChangesAsync(); r.OperacionId = op.Id; r.Estado = "Autorizada"; await db.SaveChangesAsync();
            foreach (var d in review.Lines) db.Add(new MovimientoInventario { OperacionId = op.Id, ProductoId = d.ProductId, Secuencia = d.Number, SolicitudInventarioDetalleId = d.DetailId, CantidadEscalada = d.Quantity, Escala = d.Scale, EstadoCosto = d.CostState, CostoUnitEscalado = d.BaseCost, EscalaCosto = d.CostScale });
        }
        operations.Audit(userId, approve ? "Aplicar solicitud de inventario" : "Rechazar solicitud de inventario", "SolicitudInventario", r.Id.ToString(), "decision-" + auth.Id, new { version, hash, motivo = reason, operacionId = r.OperacionId }, authorizationId: auth.Id);
        await db.SaveChangesAsync(); await tx.CommitAsync(); return r.OperacionId;
    }
    public async Task CancelAsync(long userId, long id, long version, string reason)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await RequireAsync(userId);
        var r = await db.Set<SolicitudInventario>().SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Solicitud no encontrada.");
        if (r.SolicitadoPor != userId || r.Estado is "Autorizada" or "Cancelada") throw new BusinessException("Esta solicitud no se puede cancelar.");
        AccessControl.Version(r.Version, version); reason = AccessControl.Text(reason, "Motivo de cancelación", 1000);
        foreach (var a in await db.Set<Autorizacion>().Where(x => x.SolicitudInventarioId == id && x.Decision == "pendiente" && x.InvalidadaEn == null).ToListAsync()) { a.InvalidadaEn = Formats.Now(); a.MotivoInvalidacion = reason; }
        r.Estado = "Cancelada"; r.ResueltoEn = Formats.Now(); r.Version++;
        operations.Audit(userId, "Cancelar solicitud de inventario", "SolicitudInventario", id.ToString(), Guid.NewGuid().ToString(), new { motivo = reason, alteraExistencias = false });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
