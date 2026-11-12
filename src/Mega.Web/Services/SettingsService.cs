using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed record SettingsChange(long Id, long Version, string Name, string Address, string? Phone,
    string? Issuer, string? IssuerId, string? FiscalRegime, string UsualFund, long? ExpirationDays, string? Printer,bool EnablePrint=false);

public sealed class SettingsService(MegaDbContext db, AccessControl access, Operations operations,IPrintTransport printing)
{
    public async Task<ConfiguracionNegocio> CurrentAsync()
    {
        var now = Formats.Now();
        return await db.Set<ConfiguracionNegocio>().AsNoTracking().SingleOrDefaultAsync(x => x.VigenteHasta == null && string.Compare(x.VigenteDesde, now) <= 0)
            ?? throw new BusinessException("No existe una configuración vigente. Requiere revisión de administración.");
    }

    public async Task<long> SaveAsync(long userId, SettingsChange input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.ManagerAsync(userId);
        var current = await CurrentAsync();
        if (current.Id != input.Id) throw new BusinessException("La configuración cambió. Recarga antes de guardar.");
        AccessControl.Version(current.Version, input.Version);
        var name = AccessControl.Text(input.Name, "Nombre del negocio");
        var address = AccessControl.Text(input.Address, "Dirección", 500);
        var phone = AccessControl.Optional(input.Phone, "Teléfono", 100);
        var issuer = AccessControl.Optional(input.Issuer, "Emisor", 200);
        var issuerId = AccessControl.Optional(input.IssuerId, "Identificación del emisor", 100);
        var regime = AccessControl.Optional(input.FiscalRegime, "Régimen fiscal", 200);
        var printer = AccessControl.Optional(input.Printer, "Impresora", 200);
        if(input.EnablePrint&&(printer is null||!printing.Printers().Contains(printer)||printer.Contains("OneNote",StringComparison.OrdinalIgnoreCase)))throw new BusinessException("Selecciona una impresora instalada compatible antes de habilitar la impresión automática.");
        var fund = Formats.Scaled(Formats.Parse(input.UsualFund), 2);
        if (input.ExpirationDays < 0) throw new BusinessException("Los días de próximo vencimiento no pueden ser negativos.");
        var old = await db.Set<ConfiguracionNegocio>().SingleAsync(x => x.Id == current.Id);
        var next = (ConfiguracionNegocio)db.Entry(old).CurrentValues.ToObject();
        var now = Formats.Now();
        if (string.CompareOrdinal(now, old.VigenteDesde) <= 0) { await Task.Delay(2); now = Formats.Now(); }
        if (string.CompareOrdinal(now, old.VigenteDesde) <= 0) throw new BusinessException("No se pudo establecer una fecha posterior. Vuelve a intentar.");
        old.VigenteHasta = now; await db.SaveChangesAsync();
        next.Id = 0; next.Version = checked(current.Version + 1); next.VigenteDesde = now; next.VigenteHasta = null; next.CambiadoPor = userId;
        next.Nombre = name; next.Direccion = address; next.Telefono = phone; next.Emisor = issuer; next.IdentificacionEmisor = issuerId;
        next.RegimenFiscal = regime; next.FondoHabitualCentavos = fund; next.DiasAlertaVencimiento = input.ExpirationDays; next.ImpresoraNombre = printer;
        if (issuer != current.Emisor || issuerId != current.IdentificacionEmisor || regime != current.RegimenFiscal) next.FiscalidadValidada = 0;
        next.ImpresionHabilitada=input.EnablePrint?1:0;
        // Medios, secretos y autorizaciones de respaldo no son campos editables de esta pantalla.
        db.Add(next); await db.SaveChangesAsync();
        operations.Audit(userId, "Actualizar configuración", "ConfiguracionNegocio", next.Id.ToString(), Guid.NewGuid().ToString(),
            new { configuracionAnteriorId = current.Id, configuracionNuevaId = next.Id, antes = Snapshot(current), despues = Snapshot(next) });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return next.Id;
    }

    private static object Snapshot(ConfiguracionNegocio c) => new
    {
        c.Version, c.Nombre, c.Direccion, c.Telefono, c.Moneda, c.Emisor, c.IdentificacionEmisor, c.RegimenFiscal,
        c.FiscalidadValidada, c.FondoHabitualCentavos, c.DiasAlertaVencimiento, c.ImpresoraNombre, c.ImpresionHabilitada
    };
}
