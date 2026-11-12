using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;

public sealed class SettingsTests
{
    private static SettingsChange Change(ConfiguracionNegocio c, string name = "MEGA configuración de prueba", string fund = "600") =>
        new(c.Id, c.Version, name, "Dirección ficticia", "0000-0000", "Emisor de prueba", null, null, fund, 7, "Impresora pendiente");
    [Fact]
    public async Task new_settings_preserve_existing_sales_and_opening_fund()
    {
        using var f = new MegaFactory(); using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MegaDbContext>(); var service = scope.ServiceProvider.GetRequiredService<SettingsService>(); var op = scope.ServiceProvider.GetRequiredService<Operations>();
        await op.OpenCashAsync(2, "480", "Diferencia de prueba"); List<LineInput> lines = [new(2, "unidad", "1")];
        var q = await op.QuoteAsync(2, lines); var sale = await op.ConfirmAsync(2, new(Guid.NewGuid().ToString(), q.Hash, "50", false, lines));
        var receipt = (await db.Set<Venta>().AsNoTracking().SingleAsync(x => x.Id == sale)).ComprobanteSnapshot;
        var old = await service.CurrentAsync(); var next = await service.SaveAsync(1, Change(old));
        Assert.NotEqual(old.Id, next); Assert.Equal("Minisúper MEGA · demostración", (await db.Set<ConfiguracionNegocio>().SingleAsync(x => x.Id == old.Id)).Nombre);
        Assert.Equal(receipt, (await db.Set<Venta>().AsNoTracking().SingleAsync(x => x.Id == sale)).ComprobanteSnapshot);
        var cash = await op.CashAsync(2); Assert.Equal(60000, cash.Usual); Assert.Equal(48000, cash.Shift!.FondoRealCentavos); Assert.Equal(51800, cash.Expected);
        var current = await service.CurrentAsync(); Assert.Equal(2, current.Version); Assert.Equal("NIO", current.Moneda); Assert.Equal(0, current.FiscalidadValidada); Assert.Equal(0, current.ImpresionHabilitada);
        Assert.Equal(0, current.TransferenciaHabilitada); Assert.Equal(0, current.TarjetaHabilitada);
    }
    [Fact]
    public async Task settings_change_invalidates_prior_quote_and_stale_edit()
    {
        using var f = new MegaFactory(); using var scope = f.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<SettingsService>(); var op = scope.ServiceProvider.GetRequiredService<Operations>();
        await op.OpenCashAsync(2, "500", null); List<LineInput> lines = [new(2, "unidad", "1")]; var quote = await op.QuoteAsync(2, lines);
        var old = await service.CurrentAsync(); await service.SaveAsync(1, Change(old));
        await Assert.ThrowsAsync<BusinessException>(() => service.SaveAsync(1, Change(old)));
        await Assert.ThrowsAsync<BusinessException>(() => op.ConfirmAsync(2, new(Guid.NewGuid().ToString(), quote.Hash, "50", false, lines)));
        var db = scope.ServiceProvider.GetRequiredService<MegaDbContext>(); Assert.Equal(2, await db.Set<ConfiguracionNegocio>().CountAsync()); Assert.Empty(await db.Set<Venta>().ToListAsync());
    }
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task operational_roles_cannot_change_settings(long user)
    {
        using var f = new MegaFactory(); using var scope = f.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<SettingsService>();
        var current = await service.CurrentAsync();
        await Assert.ThrowsAsync<BusinessException>(() => service.SaveAsync(user, Change(current)));
    }
    [Fact]
    public async Task invalid_fund_or_alert_does_not_close_current_settings()
    {
        using var f = new MegaFactory(); using var scope = f.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<SettingsService>(); var old = await service.CurrentAsync();
        await Assert.ThrowsAsync<BusinessException>(() => service.SaveAsync(1, Change(old, fund: "-1")));
        await Assert.ThrowsAsync<BusinessException>(() => service.SaveAsync(1, Change(old) with { ExpirationDays = -1 }));
        Assert.Equal(old.Id, (await service.CurrentAsync()).Id);
    }
    [Fact]
    public async Task settings_form_is_operational_and_rejects_inventory_access()
    {
        using var f = new MegaFactory(); using var client = f.Client(); await HttpTestSupport.LoginAsync(client);
        var response = await HttpTestSupport.PostAsync(client, "/Configuracion", new() { ["Input.Id"] = "1", ["Input.Version"] = "1", ["Input.Name"] = "MEGA desde formulario", ["Input.Address"] = "Dirección de prueba", ["Input.UsualFund"] = "700", ["Input.ExpirationDays"] = "" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = f.Services.CreateScope(); var config = await scope.ServiceProvider.GetRequiredService<SettingsService>().CurrentAsync(); Assert.Equal("MEGA desde formulario", config.Nombre); Assert.Null(config.DiasAlertaVencimiento);
        using var inventory = f.Client(); await HttpTestSupport.LoginAsync(inventory, "inventario"); Assert.Equal(HttpStatusCode.Redirect, (await inventory.GetAsync("/Configuracion")).StatusCode);
    }
}
