using Mega.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Mega.Web.Pages;
[Authorize(Policy="Gestion")]
public sealed class BitacoraModel(MegaDbContext db):PageModel
{
    public List<EventoAuditoria> Rows {get;private set;}=[];
    public Dictionary<long,string> Users {get;private set;}=[];
    [BindProperty(SupportsGet=true)]public string? Result{get;set;}
    [BindProperty(SupportsGet=true)]public string? Search{get;set;}
    public async Task OnGetAsync(){var query=db.Set<EventoAuditoria>().AsNoTracking();if(Result is "exito" or "rechazado" or "fallo" or "pendiente" or "incierto")query=query.Where(x=>x.Resultado==Result);if(!string.IsNullOrWhiteSpace(Search)){var term=Search.Trim();query=query.Where(x=>x.Accion.Contains(term)||x.Entidad.Contains(term)||x.ClaveOperacion.Contains(term));}Rows=await query.OrderByDescending(x=>x.Id).Take(100).ToListAsync();Users=await db.Set<Usuario>().AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.NombreCuenta);}
}
