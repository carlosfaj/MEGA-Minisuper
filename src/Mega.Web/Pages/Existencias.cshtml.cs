using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Mega.Web.Pages;
[Authorize(Policy="Inventario")]
public sealed class ExistenciasModel(MegaDbContext db,Operations operations):PageModel
{
    public sealed record StockRow(string Name,string Unit,decimal Stock,decimal Minimum);
    public List<StockRow> Rows {get;private set;}=[];
    public async Task OnGetAsync()
    {
        var stocks=await operations.StocksAsync();var products=await db.Set<Producto>().AsNoTracking().Where(x=>x.Activo==1).OrderBy(x=>x.Nombre).ToListAsync();
        Rows=products.Select(x=>new StockRow(x.Nombre,x.UnidadBase,stocks.GetValueOrDefault(x.Id),x.MinimoEscalado/(decimal)Math.Pow(10,x.EscalaCantidad))).ToList();
    }
}
