using Mega.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Mega.Web.Pages;
[Authorize(Policy="Gestion")]
public sealed class BitacoraModel(MegaDbContext db):PageModel
{
    public List<EventoAuditoria> Rows {get;private set;}=[];
    public Dictionary<long,string> Users {get;private set;}=[];
    public async Task OnGetAsync(){Rows=await db.Set<EventoAuditoria>().AsNoTracking().OrderByDescending(x=>x.Id).Take(100).ToListAsync();Users=await db.Set<Usuario>().AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.NombreCuenta);}
}
