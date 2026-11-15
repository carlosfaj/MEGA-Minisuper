using System.Security.Claims;
using Mega.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Mega.Web.Pages;
public sealed class IndexModel(Operations operations, HomeDashboardService dashboards):PageModel
{
    public CashSummary? Cash {get;private set;}
    public HomeDashboard? Dashboard { get; private set; }
    public async Task OnGetAsync()
    {
        var id = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (User.HasClaim("caja", "1")) Cash = await operations.CashAsync(id);
        if (User.IsInRole("Inventario") || User.IsInRole("Administrador") || User.IsInRole("Propietario")) Dashboard = await dashboards.ReadAsync(id);
    }
}
