using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages;
[Authorize(Policy="Anulaciones")]
public sealed class ImpresionesModel(MegaDbContext db,PrintQueue queue,IPrintTransport transport):ManagementPage
{
    [BindProperty(SupportsGet=true)]public long? SaleId{get;set;}
    public List<SolicitudImpresion> Jobs{get;private set;}=[];
    public List<IntentoImpresion> Attempts{get;private set;}=[];
    public List<EntregaComprobante> Deliveries{get;private set;}=[];
    public Dictionary<long,string> Folios{get;private set;}=[];
    public IReadOnlyList<string> Printers{get;private set;}=[];
    public bool Manager=>User.IsInRole("Administrador")||User.IsInRole("Propietario");
    public async Task<IActionResult> OnGetAsync(){try{await LoadAsync();return Page();}catch(BusinessException){return Forbid();}}
    public async Task<IActionResult> OnPostCopyAsync(long saleId,string key,string reason)=>await HandleAsync(async()=>{await queue.CopyAsync(UserId,saleId,key,reason);TempData["Message"]="Copia solicitada con el mismo folio e importes; no se repitió la venta.";},saleId);
    public async Task<IActionResult> OnPostResolveAsync(long id,string hash,string resolution,string reason,string evidence,bool checkedQueue)=>await HandleAsync(async()=>{await queue.ResolveAsync(UserId,id,hash,resolution,reason,evidence,checkedQueue);TempData["Message"]="Resolución registrada. Revisa el estado y las entregas.";},SaleId);
    public async Task<IActionResult> OnPostDeliverAsync(long id,string evidence,bool handedOver)=>await HandleAsync(async()=>{await queue.DeliverAsync(UserId,id,evidence,handedOver);TempData["Message"]="Entrega física declarada registrada.";},SaleId);
    private async Task<IActionResult> HandleAsync(Func<Task> action,long? saleId)
    {try{if(ModelState.IsValid){await action();return RedirectToPage(new{saleId});}}catch(BusinessException error){ModelState.AddModelError("",error.Message);}SaleId=saleId;await LoadAsync();return Page();}
    private async Task LoadAsync()
    {
        var jobs=db.Set<SolicitudImpresion>().AsNoTracking();
        if(SaleId is long id){await queue.RequireSaleAsync(UserId,id);jobs=jobs.Where(x=>x.VentaId==id);}
        else if(!Manager){var own=db.Set<TurnoCaja>().Where(x=>x.OperadorId==UserId&&x.Estado=="Abierto").Select(x=>x.Id);var sales=db.Set<Venta>().Where(x=>own.Contains(x.TurnoId)).Select(x=>x.Id);jobs=jobs.Where(x=>sales.Contains(x.VentaId));}
        Jobs=await jobs.OrderByDescending(x=>x.Id).Take(100).ToListAsync();var ids=Jobs.Select(x=>x.Id).ToArray();var saleIds=Jobs.Select(x=>x.VentaId).ToArray();
        Attempts=await db.Set<IntentoImpresion>().AsNoTracking().Where(x=>ids.Contains(x.SolicitudImpresionId)).OrderBy(x=>x.Numero).ToListAsync();
        Deliveries=await db.Set<EntregaComprobante>().AsNoTracking().Where(x=>saleIds.Contains(x.VentaId)).ToListAsync();Folios=await db.Set<Venta>().Where(x=>saleIds.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.Folio);
        if(Manager)try{Printers=transport.Printers();}catch(PrintDispatchException error){ModelState.AddModelError("",error.Message);}
    }
}
