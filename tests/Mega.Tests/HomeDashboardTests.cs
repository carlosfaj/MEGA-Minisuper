using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;

public sealed class HomeDashboardTests
{
    [Theory]
    [InlineData("inventario", "Trabajo de inventario")]
    [InlineData("admin", "Operación del día")]
    [InlineData("propietario", "Resumen del negocio")]
    public async Task homes_have_distinct_jobs_and_do_not_mutate_business_data(string account, string heading)
    {
        using var f = new MegaFactory(); using var client = f.Client();
        await HttpTestSupport.LoginAsync(client, account);
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains(heading, html);
        Assert.DoesNotContain("Según permisos", html);
        using var s = f.Services.CreateScope(); var db = s.ServiceProvider.GetRequiredService<MegaDbContext>();
        Assert.Empty(await db.Set<Venta>().ToListAsync());
        Assert.Single(await db.Set<OperacionInventario>().ToListAsync());
    }

    [Fact]
    public async Task inventory_has_own_work_and_quantities_without_financial_data_or_review_actions()
    {
        using var f = new MegaFactory(); using var s = f.Services.CreateScope();
        var db = s.ServiceProvider.GetRequiredService<MegaDbContext>();
        var p = await db.Set<Producto>().SingleAsync(x => x.Id == 2); p.MinimoEscalado = 30; await db.SaveChangesAsync();
        var receiving = s.ServiceProvider.GetRequiredService<ReceivingService>();
        var receipt = await receiving.SaveAsync(3, new(null, 0, Guid.NewGuid().ToString(), "REC-MIA", null, null, null, null, []));
        await receiving.SaveAsync(1, new(null, 0, Guid.NewGuid().ToString(), "REC-AJENA", null, null, null, null, []));
        var summary = await s.ServiceProvider.GetRequiredService<HomeDashboardService>().ReadAsync(3);
        Assert.Null(summary.Results); Assert.Empty(summary.Shifts);
        Assert.Equal(30m, Assert.Single(summary.LowStock).Quantity);
        var task = Assert.Single(summary.Tasks); Assert.Equal(receipt, task.Id); Assert.Equal("REC-MIA", task.Reference); Assert.False(task.CanReview);
        using var client = f.Client(); await HttpTestSupport.LoginAsync(client, "inventario");
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/"));
        Assert.Contains("REC-MIA", html); Assert.DoesNotContain("REC-AJENA", html);
        Assert.DoesNotContain("Margen bruto", html); Assert.DoesNotContain("C$", html); Assert.DoesNotContain("/Revision?", html);
        await Assert.ThrowsAsync<BusinessException>(() => s.ServiceProvider.GetRequiredService<HomeDashboardService>().ReadAsync(2));
    }

    [Fact]
    public async Task only_live_reviews_appear_and_own_request_requires_another_account()
    {
        using var f = new MegaFactory(); using var s = f.Services.CreateScope();
        var db = s.ServiceProvider.GetRequiredService<MegaDbContext>();
        var receiving = s.ServiceProvider.GetRequiredService<ReceivingService>();
        var supplier = await receiving.SaveSupplierAsync(1, new(null, 0, "Proveedor de prueba", null, null, null, null, true));
        var id = await receiving.SaveAsync(3, new(null, 0, Guid.NewGuid().ToString(), "REC-REVISAR", supplier, "DOC-PRUEBA", "2026-11-15", null, [new(2, "1", "1", "10", null, null, null)]));
        await receiving.SubmitAsync(3, id, 1);
        var dashboards = s.ServiceProvider.GetRequiredService<HomeDashboardService>();
        var manager = await dashboards.ReadAsync(1); Assert.True(Assert.Single(manager.Tasks).CanReview);
        var author = await dashboards.ReadAsync(3); Assert.Equal("Espera otra cuenta", Assert.Single(author.Tasks).State);
        await receiving.SaveAsync(3, new(id, 1, (await db.Set<Recepcion>().AsNoTracking().SingleAsync(x => x.Id == id)).ClaveSolicitud, "REC-REVISAR", null, null, null, null, [new(2, "2", "2", "10", null, null, null)]));
        Assert.Empty((await dashboards.ReadAsync(1)).Tasks);
    }

    [Fact]
    public async Task expiration_alerts_use_configured_threshold_and_nonempty_lot_balances()
    {
        using var f = new MegaFactory(); using var s = f.Services.CreateScope();
        var db = s.ServiceProvider.GetRequiredService<MegaDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6)); var expiry = today.AddDays(5).ToString("yyyy-MM-dd");
        var receiving = s.ServiceProvider.GetRequiredService<ReceivingService>();
        var supplier = await receiving.SaveSupplierAsync(1, new(null, 0, "Proveedor lotes", null, null, null, null, true));
        var id = await receiving.SaveAsync(3, new(null, 0, Guid.NewGuid().ToString(), "REC-LOTE", supplier, "DOC-LOTE", today.ToString("yyyy-MM-dd"), null, [new(2, "2", "2", "10", expiry, "LOTE-PRUEBA", null)]));
        var a = await receiving.SubmitAsync(3, id, 1); var review = await db.Set<Autorizacion>().AsNoTracking().SingleAsync(x => x.Id == a);
        await receiving.DecideAsync(1, a, 1, review.ContenidoHash, true, "Comprobación ficticia");
        var dashboards = s.ServiceProvider.GetRequiredService<HomeDashboardService>();
        Assert.Empty((await dashboards.ReadAsync(3, today)).LotAlerts);
        var settings = s.ServiceProvider.GetRequiredService<SettingsService>(); var c = await settings.CurrentAsync();
        await settings.SaveAsync(1, new(c.Id, c.Version, c.Nombre, c.Direccion, c.Telefono, c.Emisor, c.IdentificacionEmisor, c.RegimenFiscal, "500", 7, c.ImpresoraNombre));
        var near = Assert.Single((await dashboards.ReadAsync(3, today)).LotAlerts); Assert.Equal("Próximo a vencer", near.State); Assert.Equal(2m, near.Quantity);
        Assert.Equal("Vencido", Assert.Single((await dashboards.ReadAsync(3, today.AddDays(6))).LotAlerts).State);
    }

    [Fact]
    public async Task daily_results_reuse_reports_unknown_cost_and_closed_cash_difference()
    {
        using var f = new MegaFactory(); using var s = f.Services.CreateScope();
        var op = s.ServiceProvider.GetRequiredService<Operations>();
        await op.OpenCashAsync(2, "500", null);
        List<LineInput> lines = [new(2, "unidad", "1")]; var quote = await op.QuoteAsync(2, lines);
        await op.ConfirmAsync(2, new(Guid.NewGuid().ToString(), quote.Hash, "100", false, lines));
        var shift = (await op.CashAsync(2)).Shift!; await op.CloseCashAsync(2, shift.Id, shift.Version, "537", "Diferencia ficticia");
        var dashboards = s.ServiceProvider.GetRequiredService<HomeDashboardService>();
        var owner = await dashboards.ReadAsync(5);
        Assert.Equal("C$ 38.00", owner.Metric("Ingreso sin impuesto")); Assert.Equal("No calculable", owner.Metric("Margen bruto"));
        Assert.Equal(1, owner.DifferentClosings); Assert.Equal(-100, Assert.Single(owner.Shifts).Difference);
        var tomorrow = await dashboards.ReadAsync(5, DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-6)).AddDays(1));
        Assert.Equal("0", tomorrow.Metric("Ventas incluidas")); Assert.Equal(0, tomorrow.DifferentClosings);
    }
}
