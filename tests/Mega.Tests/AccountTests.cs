using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;

public sealed class AccountTests
{
    private const string Secret = "NuevaCuenta2026!";
    private sealed class Fixture : IDisposable
    {
        public MegaFactory Factory { get; } = new();
        public IServiceScope Scope { get; }
        public MegaDbContext Db => Scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        public AccountService Service => Scope.ServiceProvider.GetRequiredService<AccountService>();
        public Operations Op => Scope.ServiceProvider.GetRequiredService<Operations>();
        public Fixture() => Scope = Factory.Services.CreateScope();
        public async Task<AccountChange> Change(long id)
        {
            Db.ChangeTracker.Clear(); var u = await Db.Set<Usuario>().AsNoTracking().SingleAsync(x => x.Id == id);
            return new(u.Id, u.CredencialesVersion, u.NombreCuenta, u.NombreVisible, u.RolId!.Value, u.PermisoOperativoCaja == 1, u.DelegacionCuentas == 1, u.Activo == 1, null);
        }
        public void Dispose() { Scope.Dispose(); Factory.Dispose(); }
    }
    [Fact]
    public async Task owner_creates_account_hashed_and_deactivated_names_are_not_reused()
    {
        using var f = new Fixture(); var id = await f.Service.SaveAsync(5, new(null, 0, "nueva.caja", "Nueva caja", 3, true, false, true, Secret));
        f.Db.ChangeTracker.Clear(); var u = await f.Db.Set<Usuario>().SingleAsync(x => x.Id == id);
        Assert.NotEqual(Secret, u.PasswordHash); Assert.Equal(PasswordVerificationResult.Success, f.Scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>().VerifyHashedPassword(u, u.PasswordHash, Secret));
        await f.Service.SaveAsync(5, (await f.Change(id)) with { Active = false });
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(5, new(null, 0, "NUEVA.CAJA", "Duplicado", 3, true, false, true, Secret)));
        f.Db.ChangeTracker.Clear(); Assert.Equal(6, await f.Db.Set<Usuario>().CountAsync());
        Assert.DoesNotContain(Secret, string.Join("", await f.Db.Set<EventoAuditoria>().Select(x => x.DatosCambio).ToListAsync()));
    }
    [Fact]
    public async Task delegation_is_explicit_and_admin_cannot_promote_owner_or_delegate()
    {
        using var f = new Fixture(); var draft = new AccountChange(null, 0, "creada", "Prueba", 3, true, false, true, Secret);
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(1, draft));
        await f.Service.SaveAsync(5, (await f.Change(1)) with { Delegated = true });
        await f.Service.SaveAsync(1, draft);
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(1, draft with { Account = "suplantado", RoleId = 1 }));
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(1, draft with { Account = "delegado", RoleId = 2, Delegated = true }));
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(1, (f.Change(5).GetAwaiter().GetResult()) with { Name = "Propietario alterado" }));
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(1, (f.Change(1).GetAwaiter().GetResult()) with { Name = "Cambio propio" }));
    }
    [Theory]
    [InlineData(2)] [InlineData(3)]
    public async Task operational_roles_cannot_manage_accounts(long id)
    {
        using var f = new Fixture(); await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(id, new(null, 0, "cuenta", "Prueba", 3, true, false, true, Secret)));
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.CreateSupportAsync(id, "soporte", Secret, 1, "Prueba"));
    }
    [Fact]
    public async Task last_owner_cannot_be_disabled_or_demoted_and_stale_edits_fail()
    {
        using var f = new Fixture(); var original = await f.Change(5);
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(5, original with { Active = false })); f.Db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(5, original with { RoleId = 2 })); f.Db.ChangeTracker.Clear();
        var second = await f.Service.SaveAsync(5, new(null, 0, "dueno2", "Segundo propietario", 1, false, false, true, Secret));
        await f.Service.SaveAsync(5, original with { Name = "Nombre actualizado" });
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(5, original)); f.Db.ChangeTracker.Clear();
        await f.Service.SaveAsync(second, (await f.Change(5)) with { Active = false });
    }
    [Fact]
    public async Task open_shift_keeps_operator_permissions_but_password_reset_remains_possible()
    {
        using var f = new Fixture(); await f.Op.OpenCashAsync(2, "500", null); var original = await f.Change(2);
        foreach (var change in new[] { original with { Active = false }, original with { Cash = false }, original with { RoleId = 2 } })
        { await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(5, change)); f.Db.ChangeTracker.Clear(); }
        await f.Service.SaveAsync(5, original with { Password = Secret });
        Assert.Equal(50000, (await f.Op.CashAsync(2)).Expected);
    }
    [Fact]
    public async Task invalid_role_permissions_or_weak_password_create_nothing()
    {
        using var f = new Fixture(); var input = new AccountChange(null, 0, "invalida", "Prueba", 3, true, false, true, Secret);
        foreach (var c in new[] { input with { RoleId = 4 }, input with { Delegated = true }, input with { Password = "corta" }, input with { Account = "usuario con espacios" } })
        { await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(5, c)); f.Db.ChangeTracker.Clear(); }
        Assert.Equal(5, await f.Db.Set<Usuario>().CountAsync());
    }
    [Fact]
    public async Task password_change_revokes_other_sessions_and_requires_current_password()
    {
        using var f = new Fixture(); using var client = f.Factory.Client(); await HttpTestSupport.LoginAsync(client, "cajero");
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.ChangePasswordAsync(2, "incorrecta", Secret)); f.Db.ChangeTracker.Clear();
        await f.Service.ChangePasswordAsync(2, "MegaDemo2026!", Secret);
        var response = await client.GetAsync("/Caja"); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Contains("/Acceso", response.Headers.Location!.ToString());
        await HttpTestSupport.LoginAsync(client, "cajero", Secret);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Caja")).StatusCode);
    }
    [Fact]
    public async Task delegation_change_revokes_cookie_and_policy_rechecks_after_login()
    {
        using var f = new Fixture(); using var client = f.Factory.Client(); await HttpTestSupport.LoginAsync(client);
        Assert.Contains("/SinPermiso", (await client.GetAsync("/Cuentas")).Headers.Location!.ToString());
        await f.Service.SaveAsync(5, (await f.Change(1)) with { Delegated = true });
        Assert.Contains("/Acceso", (await client.GetAsync("/Cuentas")).Headers.Location!.ToString());
        await HttpTestSupport.LoginAsync(client);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Cuentas")).StatusCode);
    }
    [Fact]
    public async Task support_has_explicit_scope_no_commercial_role_and_revocation_kills_cookie()
    {
        using var f = new Fixture(); var id = await f.Service.CreateSupportAsync(5, "tecnico", Secret, 1, "Revisión ficticia");
        var user = await f.Db.Set<Usuario>().AsNoTracking().SingleAsync(x => x.Id == id); Assert.Null(user.RolId); Assert.Equal(0, user.PermisoOperativoCaja);
        using var client = f.Factory.Client(); await HttpTestSupport.LoginAsync(client, "tecnico", Secret);
        var diagnostic = await client.GetAsync("/Soporte"); Assert.Equal(HttpStatusCode.OK, diagnostic.StatusCode); Assert.Contains("Correcta", await diagnostic.Content.ReadAsStringAsync());
        foreach (var path in new[] { "/Cuentas", "/Configuracion", "/Caja", "/Venta", "/Existencias", "/Bitacora", "/Catalogo", "/MiCuenta" })
            Assert.Contains("/SinPermiso", (await client.GetAsync(path)).Headers.Location!.ToString());
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(id, new(null, 0, "ataque", "Prueba", 1, true, false, true, Secret)));
        f.Db.ChangeTracker.Clear(); var grant = await f.Db.Set<AccesoSoporte>().SingleAsync(x => x.UsuarioId == id);
        await f.Service.RevokeSupportAsync(5, grant.Id, "Prueba terminada");
        Assert.Contains("/Acceso", (await client.GetAsync("/Soporte")).Headers.Location!.ToString());
    }
    [Fact]
    public async Task support_expiration_is_checked_on_every_request_and_login()
    {
        using var f = new Fixture(); var id = await f.Service.CreateSupportAsync(5, "vencido", Secret, 1, "Revisión ficticia");
        using var client = f.Factory.Client(); await HttpTestSupport.LoginAsync(client, "vencido", Secret);
        var user = await f.Db.Set<Usuario>().SingleAsync(x => x.Id == id); user.FinAcceso = "2020-01-01T00:00:00.000Z"; await f.Db.SaveChangesAsync();
        Assert.Contains("/Acceso", (await client.GetAsync("/Soporte")).Headers.Location!.ToString());
        var response = await HttpTestSupport.PostAsync(client, "/Acceso", new() { ["Account"] = "vencido", ["Password"] = Secret }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("no v&#xE1;lidos", await response.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task real_account_form_creates_and_self_password_form_signs_out()
    {
        using var f = new Fixture(); using var client = f.Factory.Client(); await HttpTestSupport.LoginAsync(client, "propietario");
        var response = await HttpTestSupport.PostAsync(client, "/Cuentas/Editar", new() { ["Input.Account"] = "formulario", ["Input.Name"] = "Cuenta formulario", ["Input.RoleId"] = "4", ["Input.Active"] = "true", ["Input.Password"] = Secret });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.True(await f.Db.Set<Usuario>().AnyAsync(x => x.NombreCuenta == "formulario"));
        using var user = f.Factory.Client(); await HttpTestSupport.LoginAsync(user, "formulario", Secret);
        response = await HttpTestSupport.PostAsync(user, "/MiCuenta", new() { ["CurrentPassword"] = Secret, ["NewPassword"] = "OtraClave2026!", ["Confirmation"] = "OtraClave2026!" });
        Assert.Equal("/Acceso", response.Headers.Location!.ToString()); Assert.Contains("/Acceso", (await user.GetAsync("/Existencias")).Headers.Location!.ToString());
    }
}
