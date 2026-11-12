using System.Security.Claims;
using Mega.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Mega.Web.Pages;
[Authorize(Policy="Caja")]
public sealed class VentasModel(MegaDbContext db):PageModel
{
    public List<Venta> Sales {get;private set;}=[];
    public async Task OnGetAsync()
    {
        var id=long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Sales=await (from sale in db.Set<Venta>().AsNoTracking() join shift in db.Set<TurnoCaja>() on sale.TurnoId equals shift.Id
            where shift.OperadorId==id && shift.Estado=="Abierto" orderby sale.Id descending select sale).ToListAsync();
    }
}
