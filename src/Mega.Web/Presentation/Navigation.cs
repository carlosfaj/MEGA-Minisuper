using System.Security.Claims;

namespace Mega.Web.Presentation;

public sealed record NavigationLink(string Label, string Page, string? Icon = null, bool Primary = false);
public sealed record NavigationGroup(string Id, string Label, string Icon, NavigationLink[] Links);

public sealed record Navigation(NavigationLink[] Shortcuts, NavigationGroup[] Groups, string Current)
{
    public bool IsActive(NavigationLink link) => link.Page != "/Index" && link.Page.EndsWith("/Index", StringComparison.Ordinal)
        ? Current.StartsWith(link.Page[..^6], StringComparison.Ordinal)
        : Current == link.Page;

    public static Navigation For(ClaimsPrincipal user, string current)
    {
        var management = user.IsInRole("Administrador") || user.IsInRole("Propietario");
        var inventory = management || user.IsInRole("Inventario");
        var cash = user.HasClaim("caja", "1");
        var cancellations = management || user.IsInRole("Cajero");
        var shortcuts = new List<NavigationLink>();
        if (cash) shortcuts.Add(new("Nueva venta", "/Venta", "cart", true));
        shortcuts.Add(new("Inicio", "/Index", "home"));
        if (cash) shortcuts.Add(new("Mi caja", "/Caja", "cash"));

        var groups = new List<NavigationGroup>();
        var sales = new List<NavigationLink>();
        if (cash) sales.Add(new("Ventas del turno", "/Ventas"));
        if (cancellations)
        {
            sales.Add(new("Anulaciones y devoluciones", "/Anulaciones/Index"));
            sales.Add(new("Impresión y entregas", "/Impresiones"));
        }
        if (sales.Count > 0) groups.Add(new("ventas", "Ventas y caja", "receipt", [.. sales]));
        if (inventory)
        {
            var links = new List<NavigationLink>
            {
                new("Existencias", "/Existencias"), new("Recepciones", "/Recepciones/Index"),
                new("Conteos físicos", "/Conteos/Index"), new("Lotes y vencimientos", "/Lotes"),
                new("Ajustes y salidas", "/Solicitudes/Index")
            };
            if (!management) links.Add(new("Proveedores", "/Proveedores"));
            groups.Add(new("inventario", "Inventario", "box", [.. links]));
        }
        if (management) groups.Add(new("catalogo", "Catálogo", "tag", [
            new("Productos", "/Catalogo/Index"), new("Precios y promociones", "/Comercial/Index"),
            new("Proveedores", "/Proveedores")
        ]));
        if (inventory)
        {
            var links = new List<NavigationLink> { new("Reportes", "/Reportes") };
            if (management) links.Add(new("Gastos operativos", "/Gastos"));
            groups.Add(new("administracion", "Administración", "chart", [.. links]));
        }
        if (management)
        {
            var links = new List<NavigationLink>
            {
                new("Configuración", "/Configuracion"), new("Correo y reportes", "/Correos"),
                new("Respaldos", "/Respaldos"), new("Bitácora", "/Bitacora")
            };
            if (user.IsInRole("Propietario")) links.Add(new("Recuperación", "/Recuperacion"));
            if (user.IsInRole("Propietario") || user.HasClaim("cuentas", "1")) links.Add(new("Cuentas", "/Cuentas/Index"));
            groups.Add(new("sistema", "Sistema", "settings", [.. links]));
        }
        if (user.HasClaim("tipo", "comercial")) groups.Add(new("cuenta", "Mi cuenta", "account", [
            new("Mi contraseña", "/MiCuenta"), new("Avance del proyecto", "/Proyecto")
        ]));
        if (user.HasClaim("tipo", "soporte_temporal")) groups.Add(new("soporte", "Soporte", "settings", [new("Diagnóstico técnico", "/Soporte")]));
        return new([.. shortcuts], [.. groups], current);
    }
}
