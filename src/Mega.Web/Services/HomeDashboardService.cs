using System.Globalization;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed record HomeStock(string Product, string Unit, decimal Quantity, decimal Minimum);
public sealed record HomeLot(string Product, string Reference, string? Expiry, string Unit, decimal Quantity, string State);
public sealed record HomeTask(string Type, string Reference, string Responsible, string Date, string State, string Page, long Id, bool CanReview);
public sealed record HomeShift(long Id, string Operator, string State, string Date, long? Difference);
public sealed class HomeDashboard
{
    public string Role { get; init; } = "";
    public string Day { get; init; } = "";
    public string GeneratedAt { get; init; } = "";
    public List<HomeStock> LowStock { get; init; } = [];
    public List<HomeLot> LotAlerts { get; init; } = [];
    public long? ExpirationDays { get; init; }
    public List<HomeTask> Tasks { get; init; } = [];
    public List<HomeShift> Shifts { get; init; } = [];
    public int OpenShifts { get; init; }
    public int DifferentClosings { get; init; }
    public int PendingRefunds { get; init; }
    public ReportSnapshot? Results { get; set; }
    public string? ResultsError { get; set; }
    public string Metric(string label) => Results?.Metrics.Single(x => x.Label == label).Value ?? "No disponible";
}

public sealed class HomeDashboardService(MegaDbContext db, AccessControl access, ReportService reports)
{
    public async Task<HomeDashboard> ReadAsync(long userId, DateOnly? date = null)
    {
        var actor = await access.RequireAsync(userId, "Inventario", "Administrador", "Propietario");
        var role = await db.Set<Rol>().Where(x => x.Id == actor.RolId).Select(x => x.Nombre).SingleAsync();
        var manager = role != "Inventario";
        var today = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6));
        var day = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var begin = day + "T06:00:00.000Z";
        var end = today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T06:00:00.000Z";
        HomeDashboard dashboard;
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var products = await db.Set<Producto>().AsNoTracking().ToDictionaryAsync(x => x.Id);
            // Sum as decimals after reading: SQLite's integer SUM can overflow before conversion.
            var moves = await db.Set<MovimientoInventario>().AsNoTracking().Select(x => new { x.ProductoId, x.LoteId, x.CantidadEscalada }).ToListAsync();
            var stocks = moves.GroupBy(x => x.ProductoId).ToDictionary(g => g.Key, g => g.Sum(x => (decimal)x.CantidadEscalada));
            var low = products.Values.Where(p => p.Activo == 1 && stocks.GetValueOrDefault(p.Id) <= p.MinimoEscalado)
                .OrderBy(p => stocks.GetValueOrDefault(p.Id) > 0).ThenBy(p => p.Nombre)
                .Select(p => new HomeStock(p.Nombre, p.UnidadBase, stocks.GetValueOrDefault(p.Id) / ReceivingService.Power(p.EscalaCantidad), p.MinimoEscalado / ReceivingService.Power(p.EscalaCantidad))).ToList();
            var now = Formats.Now();
            var days = await db.Set<ConfiguracionNegocio>().Where(x => x.VigenteHasta == null && string.Compare(x.VigenteDesde, now) <= 0).Select(x => x.DiasAlertaVencimiento).SingleAsync();
            var lotStocks = moves.Where(x => x.LoteId != null).GroupBy(x => x.LoteId!.Value).ToDictionary(g => g.Key, g => g.Sum(x => (decimal)x.CantidadEscalada));
            List<HomeLot> alerts = [];
            foreach (var lot in await db.Set<LoteExistencia>().AsNoTracking().OrderBy(x => x.Vencimiento).ThenBy(x => x.Id).ToListAsync())
            {
                var p = products[lot.ProductoId];
                var quantity = lotStocks.GetValueOrDefault(lot.Id);
                if (quantity == 0) continue;
                var state = quantity < 0 ? "Discrepancia" : "";
                if (quantity > 0)
                {
                    if (lot.Vencimiento is null && p.ControlVencimiento == 1) state = "Falta vencimiento";
                    else if (lot.Vencimiento is not null)
                    {
                        if (!DateOnly.TryParseExact(lot.Vencimiento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var expiry)) state = "Fecha por revisar";
                        else if (expiry < today) state = "Vencido";
                        else if (days is long threshold && (long)expiry.DayNumber - today.DayNumber <= threshold) state = "Próximo a vencer";
                    }
                }
                if (state.Length > 0) alerts.Add(new(p.Nombre, lot.Referencia ?? $"Lote #{lot.Id}", lot.Vencimiento, p.UnidadBase, quantity / ReceivingService.Power(p.EscalaCantidad), state));
            }
            foreach (var group in moves.Where(x => x.LoteId == null).GroupBy(x => x.ProductoId))
            {
                var p = products[group.Key]; var quantity = group.Sum(x => (decimal)x.CantidadEscalada);
                if (p.ControlVencimiento == 1 && quantity != 0) alerts.Add(new(p.Nombre, "Saldo sin lote", null, p.UnidadBase, quantity / ReceivingService.Power(p.EscalaCantidad), "Conciliar lote y vencimiento"));
            }
            var tasks = await TasksAsync(userId, manager);
            List<HomeShift> shifts = [];
            var open = 0;
            var different = 0;
            var refunds = 0;
            if (manager)
            {
                shifts = await (from t in db.Set<TurnoCaja>().AsNoTracking() join u in db.Set<Usuario>() on t.OperadorId equals u.Id
                    where t.Estado == "Abierto" || t.Cierre != null && string.Compare(t.Cierre, begin) >= 0 && string.Compare(t.Cierre, end) < 0 && t.DiferenciaCentavos != null && t.DiferenciaCentavos != 0
                    orderby t.Apertura descending select new HomeShift(t.Id, u.NombreVisible, t.Estado, t.Cierre ?? t.Apertura, t.DiferenciaCentavos)).ToListAsync();
                open = shifts.Count(x => x.State == "Abierto");
                different = shifts.Count(x => x.State != "Abierto");
                refunds = await db.Set<SolicitudAnulacion>().CountAsync(x => x.Estado == "Autorizada" && x.EfectivoADevolverCentavos > x.EfectivoDevueltoCentavos);
            }
            dashboard = new() { Role = role, Day = day, GeneratedAt = now, LowStock = low, LotAlerts = alerts, ExpirationDays = days, Tasks = tasks, Shifts = shifts, OpenShifts = open, DifferentClosings = different, PendingRefunds = refunds };
            await tx.CommitAsync();
        }
        if (manager)
        {
            try { dashboard.Results = await reports.RunAsync(userId, new(role == "Propietario" ? "resultados" : "ventas", day, day)); }
            catch (BusinessException e) { dashboard.ResultsError = e.Message; }
        }
        return dashboard;
    }

    private async Task<List<HomeTask>> TasksAsync(long userId, bool manager)
    {
        var pending = await db.Set<Autorizacion>().AsNoTracking().Where(x => x.Decision == "pendiente" && x.InvalidadaEn == null && (manager || x.SolicitanteId == userId)).ToListAsync();
        var users = await db.Set<Usuario>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.NombreVisible);
        var receiptIds = pending.Where(x => x.RecepcionId != null).Select(x => x.RecepcionId!.Value).ToArray();
        var requestIds = pending.Where(x => x.SolicitudInventarioId != null).Select(x => x.SolicitudInventarioId!.Value).ToArray();
        var receipts = await db.Set<Recepcion>().AsNoTracking().Where(x => receiptIds.Contains(x.Id) || !manager && x.CreadoPor == userId && x.Estado != "Confirmada").ToListAsync();
        var requests = await db.Set<SolicitudInventario>().AsNoTracking().Where(x => requestIds.Contains(x.Id) || !manager && x.SolicitadoPor == userId && x.Estado != "Autorizada" && x.Estado != "Cancelada").ToListAsync();
        List<HomeTask> tasks = [];
        foreach (var r in receipts) Add("Recepción", r.Referencia, r.CreadoPor, r.CreadoEn, r.Estado, "/Recepciones", r.Id, pending.FirstOrDefault(a => a.RecepcionId == r.Id));
        foreach (var r in requests) Add(r.Tipo == "ajuste" ? "Ajuste de conteo" : r.Tipo == "salida_justificada" ? "Salida justificada" : "Carga de inventario", r.Referencia, r.SolicitadoPor, r.CreadoEn, r.Estado, "/Solicitudes", r.Id, pending.FirstOrDefault(a => a.SolicitudInventarioId == r.Id));
        if (manager)
        {
            var ids = pending.Where(x => x.SolicitudAnulacionId != null).Select(x => x.SolicitudAnulacionId!.Value).ToArray();
            foreach (var r in await db.Set<SolicitudAnulacion>().AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync())
                Add("Anulación", r.Referencia, r.SolicitadoPor, r.CreadoEn, r.Estado, "/Anulaciones", r.Id, pending.First(a => a.SolicitudAnulacionId == r.Id));
        }
        else
        {
            foreach (var c in await db.Set<Conteo>().AsNoTracking().Where(x => x.RegistradoPor == userId && x.Estado == "En_registro").ToListAsync())
                tasks.Add(new("Conteo físico", c.Referencia, users[c.RegistradoPor], c.CreadoEn, "En registro", "/Conteos/Editar", c.Id, false));
        }
        return tasks.OrderBy(x => x.Date, StringComparer.Ordinal).ThenBy(x => x.Reference, StringComparer.Ordinal).ToList();

        void Add(string type, string reference, long author, string created, string state, string area, long id, Autorizacion? review)
        {
            var canReview = manager && review is not null && author != userId;
            tasks.Add(new(type, reference, users[author], review?.FechaSolicitud ?? created,
                review is not null ? canReview ? "Por revisar" : "Espera otra cuenta" : state == "Pendiente" ? "En preparación" : state.Replace('_', ' '),
                area + (canReview ? "/Revision" : "/Editar"), canReview ? review!.Id : id, canReview));
        }
    }
}
