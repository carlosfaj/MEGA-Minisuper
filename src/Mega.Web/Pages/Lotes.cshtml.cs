using System.Globalization;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages;

[Authorize(Policy = "Inventario")]
public sealed class LotesModel(MegaDbContext db, SettingsService settings) : ManagementPage
{
    public sealed record Row(long Id, string Product, string Unit, string? Reference, string? Expiry, decimal Stock, string State, long? ReceiptId);
    public sealed record Missing(string Product, string Unit, decimal Stock);
    public List<Row> Rows { get; private set; } = [];
    public List<Missing> WithoutLot { get; private set; } = [];
    public long? ExpirationDays { get; private set; }
    [BindProperty(SupportsGet = true)] public bool IncludeEmpty { get; set; }
    public async Task OnGetAsync()
    {
        ExpirationDays = (await settings.CurrentAsync()).DiasAlertaVencimiento;
        var products = await db.Set<Producto>().AsNoTracking().ToDictionaryAsync(x => x.Id);
        var totals = await db.Set<MovimientoInventario>().AsNoTracking().Where(x => x.LoteId != null).GroupBy(x => x.LoteId!.Value).Select(g => new { Id = g.Key, Quantity = g.Sum(x => x.CantidadEscalada) }).ToDictionaryAsync(x => x.Id, x => x.Quantity);
        var details = await db.Set<RecepcionDetalle>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.RecepcionId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6));
        foreach (var lot in await db.Set<LoteExistencia>().AsNoTracking().OrderBy(x => x.Vencimiento).ThenBy(x => x.Id).ToListAsync())
        {
            var p = products[lot.ProductoId]; var balance = totals.GetValueOrDefault(lot.Id); if (!IncludeEmpty && balance == 0) continue;
            var state = balance < 0 ? "Discrepancia" : balance == 0 ? "Agotado" : "Disponible";
            if (balance > 0)
            {
                if (lot.Vencimiento is null) state = p.ControlVencimiento == 1 ? "Falta vencimiento" : "Sin fecha";
                else if (!DateOnly.TryParseExact(lot.Vencimiento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry)) state = "Fecha por revisar";
                else if (expiry < today) state = "Vencido";
                else if (ExpirationDays is long days && (long)expiry.DayNumber - today.DayNumber <= days) state = "Próximo a vencer";
            }
            Rows.Add(new(lot.Id, p.Nombre, p.UnidadBase, lot.Referencia, lot.Vencimiento, balance / ReceivingService.Power(p.EscalaCantidad), state, lot.RecepcionDetalleId is long d ? details.GetValueOrDefault(d) : null));
        }
        foreach (var item in await db.Set<MovimientoInventario>().AsNoTracking().Where(x => x.LoteId == null).GroupBy(x => x.ProductoId).Select(g => new { Id = g.Key, Quantity = g.Sum(x => x.CantidadEscalada) }).ToListAsync())
        { var p = products[item.Id]; if (p.ControlVencimiento == 1 && item.Quantity != 0) WithoutLot.Add(new(p.Nombre, p.UnidadBase, item.Quantity / ReceivingService.Power(p.EscalaCantidad))); }
    }
}
