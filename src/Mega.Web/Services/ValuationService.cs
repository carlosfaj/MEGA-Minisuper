using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed record ValuationMovement(long Id, long Quantity, long Scale, string State, long? Cost, long? CostScale, long OperationSequence, long Sequence);
public sealed record ValuationState(long Balance, long Scale, string State, long? Cost, long? ReferenceId, string Fingerprint);
public sealed record StockAllocation(long? LotId, long Quantity);

/// <summary>Promedio ponderado móvil propuesto en el diseño, con precisión de costo de seis decimales.</summary>
public sealed class ValuationService(MegaDbContext db)
{
    public const string Policy = "promedio_ponderado_movil_v1";
    public static ValuationState Calculate(long scale, IReadOnlyList<ValuationMovement> input)
    {
        long balance = 0; long? cost = null, reference = null;
        try
        {
            foreach (var m in input.OrderBy(x => x.OperationSequence).ThenBy(x => x.Sequence).ThenBy(x => x.Id))
            {
                if (m.Scale != scale) throw new BusinessException("La historia contiene unidades incompatibles; requiere conciliación.");
                var next = checked(balance + m.Quantity);
                long? incoming = m.State == "conocido" && m.Cost is long c && m.CostScale is long s ? Formats.Scaled(c / ReceivingService.Power(s), 6) : null;
                if (m.Quantity > 0)
                {
                    if (balance == 0) cost = incoming;
                    else if (balance > 0 && cost is long previous && incoming is long entry)
                        cost = checked((long)decimal.Round(previous * (balance / (decimal)next) + entry * (m.Quantity / (decimal)next), 0, MidpointRounding.AwayFromZero));
                    else cost = null;
                    reference = cost is null ? null : m.Id;
                }
                else if (next <= 0 || incoming is null) { cost = null; reference = null; }
                balance = next;
            }
        }
        catch (OverflowException) { throw new BusinessException("La valoración supera la capacidad de registro. Revisa cantidades y costos."); }
        return new(balance, scale, cost is null ? "desconocido" : "conocido", cost, reference, Formats.Hash(Formats.Json(new { Policy, scale, movimientos = input })));
    }
    public async Task<ValuationState> StateAsync(long productId)
    {
        var product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == productId);
        var rows = await (from m in db.Set<MovimientoInventario>().AsNoTracking()
                          join o in db.Set<OperacionInventario>().AsNoTracking() on m.OperacionId equals o.Id
                          where m.ProductoId == productId orderby o.SecuenciaConfirmacion, m.Secuencia, m.Id
                          select new ValuationMovement(m.Id, m.CantidadEscalada, m.Escala, m.EstadoCosto, m.CostoUnitEscalado, m.EscalaCosto, o.SecuenciaConfirmacion, m.Secuencia)).ToListAsync();
        return Calculate(product.EscalaCantidad, rows);
    }
    public async Task<List<List<StockAllocation>>> AllocateAsync(Producto product, IReadOnlyList<long> quantities)
    {
        var lots = await db.Set<LoteExistencia>().AsNoTracking().Where(x => x.ProductoId == product.Id).ToListAsync();
        var balances = await db.Set<MovimientoInventario>().AsNoTracking().Where(x => x.ProductoId == product.Id).GroupBy(x => x.LoteId)
            .Select(g => new { Lot = g.Key, Quantity = g.Sum(x => x.CantidadEscalada) }).ToListAsync();
        var today = DateTime.UtcNow.AddHours(-6).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var buckets = lots.Where(l => l.Vencimiento is null ? product.ControlVencimiento == 0 :
                DateOnly.TryParseExact(l.Vencimiento, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _) && string.CompareOrdinal(l.Vencimiento, today) >= 0)
            .OrderBy(l => l.Vencimiento ?? "9999-12-31").ThenBy(l => l.Id)
            .Select(l => (Id: (long?)l.Id, Balance: Math.Max(0, balances.SingleOrDefault(x => x.Lot == l.Id)?.Quantity ?? 0))).ToList();
        if (product.ControlVencimiento == 0) buckets.Add((null, Math.Max(0, balances.SingleOrDefault(x => x.Lot == null)?.Quantity ?? 0)));
        List<List<StockAllocation>> result = [];
        foreach (var quantity in quantities)
        {
            var remaining = quantity; List<StockAllocation> line = [];
            for (var i = 0; i < buckets.Count && remaining > 0; i++)
            {
                var bucket = buckets[i]; var take = Math.Min(remaining, bucket.Balance); if (take == 0) continue;
                line.Add(new(bucket.Id, take)); remaining -= take; buckets[i] = (bucket.Id, bucket.Balance - take);
            }
            if (remaining > 0)
            {
                if (product.ControlVencimiento == 1 || lots.Count > 0) throw new BusinessException($"{product.Nombre}: no hay cantidad suficiente en lotes disponibles y no vencidos. Revisa lotes y existencias.");
                // La venta básica sin lotes conserva la contingencia física explícita del diseño.
                if (line.Count == 1 && line[0].LotId is null) line[0] = line[0] with { Quantity = checked(line[0].Quantity + remaining) };
                else line.Add(new(null, remaining));
            }
            result.Add(line);
        }
        return result;
    }
}
