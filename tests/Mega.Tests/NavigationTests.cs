using System.Security.Claims;
using Mega.Web.Presentation;

namespace Mega.Tests;

public sealed class NavigationTests
{
    private static ClaimsPrincipal Account(string role, bool cash = false, bool delegated = false, string type = "comercial") =>
        new(new ClaimsIdentity([new(ClaimTypes.Role, role), new("caja", cash ? "1" : "0"), new("cuentas", delegated ? "1" : "0"), new("tipo", type)], "test"));

    private static string[] Pages(Navigation navigation) => navigation.Shortcuts.Concat(navigation.Groups.SelectMany(group => group.Links)).Select(link => link.Page).ToArray();

    [Theory]
    [InlineData("Cajero", false)]
    [InlineData("Cajero", true)]
    [InlineData("Administrador", false)]
    [InlineData("Administrador", true)]
    public void Cash_shortcuts_require_the_operational_claim(string role, bool cash)
    {
        var pages = Pages(Navigation.For(Account(role, cash), "/Index"));
        Assert.Equal(cash, pages.Contains("/Venta"));
        Assert.Equal(cash, pages.Contains("/Caja"));
        Assert.Equal(cash, pages.Contains("/Ventas"));
    }

    [Theory]
    [InlineData("Cajero", false, false, false)]
    [InlineData("Inventario", false, true, false)]
    [InlineData("Administrador", false, true, false)]
    [InlineData("Administrador", true, true, true)]
    [InlineData("Propietario", false, true, true)]
    public void Navigation_preserves_role_and_delegation_boundaries(string role, bool delegated, bool inventory, bool accounts)
    {
        var pages = Pages(Navigation.For(Account(role, delegated: delegated), "/Index"));
        Assert.Equal(inventory, pages.Contains("/Existencias"));
        Assert.Equal(accounts, pages.Contains("/Cuentas/Index"));
        Assert.Equal(role == "Propietario", pages.Contains("/Recuperacion"));
        Assert.Equal(role is "Propietario" or "Administrador", pages.Contains("/Configuracion"));
    }

    [Fact]
    public void Temporary_support_has_only_its_own_navigation()
    {
        var pages = Pages(Navigation.For(Account("", type: "soporte_temporal"), "/Soporte"));
        Assert.Equal(new[] { "/Index", "/Soporte" }, pages);
    }

    [Fact]
    public void Detail_page_selects_its_area_without_selecting_home()
    {
        var navigation = Navigation.For(Account("Propietario"), "/Catalogo/Producto");
        Assert.False(navigation.IsActive(navigation.Shortcuts.Single(link => link.Page == "/Index")));
        Assert.Contains(navigation.Groups.Single(group => group.Id == "catalogo").Links, navigation.IsActive);
        Assert.DoesNotContain(navigation.Groups.Single(group => group.Id == "inventario").Links, navigation.IsActive);
    }
}
