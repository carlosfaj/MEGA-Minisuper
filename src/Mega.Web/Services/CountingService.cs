using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed record CountingLine(long ProductId, long? LotId, string Physical, string? Note);
public sealed record CountingChange(long? Id, long Version, string Reference, string? Notes, bool PhysicalChecked, List<CountingLine> Lines);
public sealed class CountingService(MegaDbContext db, AccessControl access, Operations operations)
{
    private Task<Usuario> RequireAsync(long id) => access.RequireAsync(id, "Inventario", "Administrador", "Propietario");
    public async Task<long> SaveAsync(long userId, CountingChange input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await RequireAsync(userId);
        if (!input.PhysicalChecked) throw new BusinessException("Confirma que comprobaste físicamente las cantidades antes de guardar.");
        var reference = AccessControl.Text(input.Reference, "Referencia", 100);
        if (await db.Set<Conteo>().AnyAsync(x => x.Id != input.Id && x.Referencia == reference)) throw new BusinessException("La referencia del conteo ya está registrada.");
        if (input.Lines is null || input.Lines.Count is 0 or >100) throw new BusinessException("El conteo requiere entre 1 y 100 líneas completas.");
        if (input.Lines.GroupBy(x => x.ProductId).Any(g => g.Select(x => x.LotId).Distinct().Count() != g.Count() || g.Any(x => x.LotId == null) && g.Count() > 1))
            throw new BusinessException("No repitas producto/lote ni mezcles el total del producto con sus lotes en un mismo conteo.");
        var count = input.Id is null ? new Conteo { RegistradoPor = userId, CreadoEn = Formats.Now(), Estado = "En_registro", Version = 1 } :
            await db.Set<Conteo>().SingleOrDefaultAsync(x => x.Id == input.Id) ?? throw new BusinessException("El conteo no existe.");
        object? before = null;
        if (input.Id is not null)
        {
            if (count.RegistradoPor != userId || count.Estado != "En_registro") throw new BusinessException("Solo quien registró un conteo sin finalizar puede editarlo.");
            AccessControl.Version(count.Version, input.Version); before = await SnapshotAsync(count.Id); count.Version++;
        }
        var details = new List<ConteoDetalle>();
        foreach (var line in input.Lines)
        {
            var product = await db.Set<Producto>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == line.ProductId) ?? throw new BusinessException("Selecciona un producto válido.");
            if (line.LotId is long lot && !await db.Set<LoteExistencia>().AnyAsync(x => x.Id == lot && x.ProductoId == product.Id)) throw new BusinessException("El lote no pertenece al producto seleccionado.");
            var physical = Formats.Scaled(Formats.Parse(line.Physical), (int)product.EscalaCantidad);
            var balance = await db.Set<MovimientoInventario>().Where(x => x.ProductoId == product.Id && (line.LotId == null || x.LoteId == line.LotId)).SumAsync(x => (long?)x.CantidadEscalada) ?? 0;
            try { _ = checked(physical - balance); } catch (OverflowException) { throw new BusinessException("La diferencia supera la capacidad de registro."); }
            details.Add(new ConteoDetalle { ProductoId = product.Id, LoteId = line.LotId, CantidadFisicaEscalada = physical, Escala = product.EscalaCantidad, SaldoReferenciaEscalado = balance, Observacion = AccessControl.Optional(line.Note, "Observación de línea", 1000) });
        }
        count.Referencia = reference; count.Observaciones = AccessControl.Optional(input.Notes, "Observaciones", 2000); count.FechaConteo = Formats.Now();
        count.OperacionReferenciaId = await db.Set<OperacionInventario>().OrderByDescending(x => x.SecuenciaConfirmacion).Select(x => (long?)x.Id).FirstOrDefaultAsync();
        if (input.Id is null) { db.Add(count); await db.SaveChangesAsync(); }
        else { db.RemoveRange(await db.Set<ConteoDetalle>().Where(x => x.ConteoId == count.Id).ToListAsync()); await db.SaveChangesAsync(); }
        foreach (var d in details) { d.ConteoId = count.Id; db.Add(d); }
        await db.SaveChangesAsync(); operations.Audit(userId, input.Id is null ? "Registrar conteo físico" : "Editar conteo físico", "Conteo", count.Id.ToString(), Guid.NewGuid().ToString(), new { antes = before, despues = await SnapshotAsync(count.Id), comprobacionFisicaDeclarada = true, alteraExistencias = false });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return count.Id;
    }
    public async Task FinalizeAsync(long userId, long id, long expectedVersion)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await RequireAsync(userId);
        var count = await db.Set<Conteo>().SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("El conteo no existe.");
        if (count.RegistradoPor != userId) throw new BusinessException("Solo quien registró el conteo puede finalizarlo.");
        if (count.Estado == "Finalizado" && (count.Version == expectedVersion || count.Version == expectedVersion + 1)) { await tx.CommitAsync(); return; }
        AccessControl.Version(count.Version, expectedVersion);
        if (count.Estado != "En_registro") throw new BusinessException("El conteo ya fue resuelto.");
        var details = await db.Set<ConteoDetalle>().AsNoTracking().Where(x => x.ConteoId == id).ToListAsync();
        if (details.Count == 0) throw new BusinessException("El conteo requiere al menos una línea.");
        var sequence = count.OperacionReferenciaId is long op ? await db.Set<OperacionInventario>().Where(x => x.Id == op).Select(x => x.SecuenciaConfirmacion).SingleAsync() : 0;
        foreach (var d in details)
        {
            if (await db.Set<MovimientoInventario>().AnyAsync(x => x.ProductoId == d.ProductoId && (d.LoteId == null || x.LoteId == d.LoteId) && db.Set<OperacionInventario>().Any(o => o.Id == x.OperacionId && o.SecuenciaConfirmacion > sequence)))
                throw new BusinessException("Hubo movimientos después de guardar las cantidades físicas. Vuelve a comprobarlas y guarda una nueva versión antes de finalizar.");
        }
        count.Estado = "Finalizado"; count.FinalizadoEn = Formats.Now(); count.Version++;
        await db.SaveChangesAsync();
        operations.Audit(userId, "Finalizar conteo físico", "Conteo", id.ToString(), Guid.NewGuid().ToString(), new { version = count.Version, alteraExistencias = false, contenido = await SnapshotAsync(id) });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
    private async Task<object> SnapshotAsync(long id)
    {
        var c = await db.Set<Conteo>().AsNoTracking().SingleAsync(x => x.Id == id);
        var lines = await db.Set<ConteoDetalle>().AsNoTracking().Where(x => x.ConteoId == id).OrderBy(x => x.Id).Select(x => new { x.ProductoId, x.LoteId, x.CantidadFisicaEscalada, x.Escala, x.SaldoReferenciaEscalado, x.Observacion }).ToListAsync();
        return new { c.Referencia, c.FechaConteo, c.RegistradoPor, c.Estado, c.Version, c.OperacionReferenciaId, c.Observaciones, lineas = lines };
    }
}
