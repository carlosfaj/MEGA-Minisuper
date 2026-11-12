using System.Security.Claims;
using System.Text.Json;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace Mega.Web.Pages;
public sealed class ComprobanteModel(MegaDbContext db):PageModel
{
    public Venta Sale {get;private set;}=null!;
    public Pago Payment {get;private set;}=null!;
    public JsonElement Snapshot {get;private set;}
    public string PrintState {get;private set;}="Pendiente";
    public async Task<IActionResult> OnGetAsync(long id)
    {
        var sale=await db.Set<Venta>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);if(sale is null)return NotFound();
        var shift=await db.Set<TurnoCaja>().AsNoTracking().SingleAsync(x=>x.Id==sale.TurnoId);
        var userId=long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var management=User.IsInRole("Administrador")||User.IsInRole("Propietario");
        if(!management && (!User.HasClaim("caja","1") || shift.OperadorId!=userId || shift.Estado!="Abierto"))return Forbid();
        if(Formats.Hash(sale.ComprobanteSnapshot)!=sale.ComprobanteHash) throw new InvalidOperationException("La integridad del comprobante requiere revisión.");
        Sale=sale;using var doc=JsonDocument.Parse(sale.ComprobanteSnapshot);Snapshot=doc.RootElement.Clone();
        Payment=await db.Set<Pago>().AsNoTracking().SingleAsync(x=>x.VentaId==id);
        PrintState=(await db.Set<SolicitudImpresion>().AsNoTracking().SingleAsync(x=>x.VentaId==id&&x.Tipo=="original")).Estado;
        return Page();
    }
}
