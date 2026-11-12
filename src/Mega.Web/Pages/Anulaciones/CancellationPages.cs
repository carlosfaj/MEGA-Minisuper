using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages.Anulaciones;
[Authorize(Policy="Anulaciones")]
public sealed class IndexModel(MegaDbContext db):ManagementPage
{
    public List<SolicitudAnulacion> Rows{get;private set;}=[];public Dictionary<long,string> Folios{get;private set;}=[];public Dictionary<long,long> Pending{get;private set;}=[];
    public async Task OnGetAsync()
    {
        var manager=User.IsInRole("Administrador")||User.IsInRole("Propietario");
        Rows=await db.Set<SolicitudAnulacion>().AsNoTracking().Where(r=>manager||r.SolicitadoPor==UserId||db.Set<Venta>().Any(v=>v.Id==r.VentaId&&db.Set<TurnoCaja>().Any(t=>t.Id==v.TurnoId&&t.OperadorId==UserId))).OrderByDescending(x=>x.Id).Take(200).ToListAsync();
        var sales=Rows.Select(x=>x.VentaId).ToList();Folios=await db.Set<Venta>().Where(x=>sales.Contains(x.Id)).ToDictionaryAsync(x=>x.Id,x=>x.Folio);
        var ids=Rows.Select(x=>x.Id).ToList();Pending=await db.Set<Autorizacion>().Where(x=>x.SolicitudAnulacionId!=null&&ids.Contains(x.SolicitudAnulacionId.Value)&&x.Decision=="pendiente"&&x.InvalidadaEn==null).ToDictionaryAsync(x=>x.SolicitudAnulacionId!.Value,x=>x.Id);
    }
}
public sealed class CancellationInput
{
    public long? Id{get;set;}public long Version{get;set;}public string Key{get;set;}=Guid.NewGuid().ToString();public long SaleId{get;set;}
    [Required,StringLength(100)]public string Reference{get;set;}="";
    [Required,StringLength(1000)]public string Reason{get;set;}="";
    [Required,StringLength(1000)]public string Evidence{get;set;}="";
    [Required(ErrorMessage="Define si se reincorpora la mercadería.")]public bool? Restock{get;set;}
    public bool PhysicalVerified{get;set;}
}
[Authorize(Policy="Anulaciones")]
public sealed class EditarModel(MegaDbContext db,CancellationService service):ManagementPage
{
    [BindProperty]public CancellationInput Input{get;set;}=new();
    [BindProperty]public string? ActionEvidence{get;set;}
    [BindProperty]public bool HandedOver{get;set;}
    public SolicitudAnulacion? Saved{get;private set;}public List<Venta> Sales{get;private set;}=[];public List<Autorizacion> Reviews{get;private set;}=[];public CancellationReview? ContentData{get;private set;}
    public bool Editable=>Saved is null||Saved.SolicitadoPor==UserId&&Saved.Estado is not("Autorizada" or "Cancelada");
    private async Task Load()
    {
        var manager=User.IsInRole("Administrador")||User.IsInRole("Propietario");Sales=await db.Set<Venta>().AsNoTracking().Where(v=>v.Estado=="Confirmada"&&(manager||db.Set<TurnoCaja>().Any(t=>t.Id==v.TurnoId&&t.OperadorId==UserId))).OrderByDescending(x=>x.Id).Take(200).ToListAsync();
        if(Input.Id is long id){Saved=await db.Set<SolicitudAnulacion>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);ContentData=await service.ReviewAsync(id);Reviews=await db.Set<Autorizacion>().AsNoTracking().Where(x=>x.SolicitudAnulacionId==id).OrderByDescending(x=>x.Id).ToListAsync();}
    }
    public async Task<IActionResult> OnGetAsync(long? id,long? saleId)
    {
        if(id is long key)
        {
            try{await service.RequireReadAsync(UserId,key);}catch(BusinessException){return Forbid();}
            var r=await db.Set<SolicitudAnulacion>().AsNoTracking().SingleAsync(x=>x.Id==key);Input=new(){Id=r.Id,Version=r.Version,Key=r.ClaveSolicitud,SaleId=r.VentaId,Reference=r.Referencia,Reason=r.Motivo??"",Evidence=r.EvidenciaReferencia??"",Restock=r.ReincorporacionEstado=="Verificada"};
        }
        else Input.SaleId=saleId??0;await Load();return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {try{if(ModelState.IsValid){var id=await service.SaveAsync(UserId,new(Input.Id,Input.Version,Input.Key,Input.SaleId,Input.Reference,Input.Reason,Input.Evidence,Input.Restock!.Value,Input.PhysicalVerified));TempData["Message"]="Preparación guardada sin anular, reincorporar ni devolver fondos.";return RedirectToPage(new{id});}}catch(BusinessException e){ModelState.AddModelError("",e.Message);}if(Input.Id is long key){try{await service.RequireReadAsync(UserId,key);}catch(BusinessException){return Forbid();}}await Load();return Page();}
    public async Task<IActionResult> OnPostSubmitAsync()
    {ModelState.Clear();try{await service.SubmitAsync(UserId,Input.Id??0,Input.Version);TempData["Message"]="Solicitud enviada para revisión por otra cuenta.";return RedirectToPage(new{id=Input.Id});}catch(BusinessException e){var result=await OnGetAsync(Input.Id,null);if(result is not Microsoft.AspNetCore.Mvc.RazorPages.PageResult)return result;ModelState.Clear();ModelState.AddModelError("",e.Message);return Page();}}
    public async Task<IActionResult> OnPostRefundAsync()
    {ModelState.Clear();try{await service.RefundAsync(UserId,Input.Id??0,Input.Version,HandedOver,ActionEvidence??"");TempData["Message"]="Devolución efectiva registrada una sola vez en tu turno actual.";return RedirectToPage(new{id=Input.Id});}catch(BusinessException e){var result=await OnGetAsync(Input.Id,null);if(result is not Microsoft.AspNetCore.Mvc.RazorPages.PageResult)return result;ModelState.Clear();ModelState.AddModelError("",e.Message);return Page();}}
    public async Task<IActionResult> OnPostCancelAsync()
    {ModelState.Clear();try{await service.CancelAsync(UserId,Input.Id??0,Input.Version,ActionEvidence??"");TempData["Message"]="Solicitud cancelada sin efectos comerciales.";return RedirectToPage(new{id=Input.Id});}catch(BusinessException e){var result=await OnGetAsync(Input.Id,null);if(result is not Microsoft.AspNetCore.Mvc.RazorPages.PageResult)return result;ModelState.Clear();ModelState.AddModelError("",e.Message);return Page();}}
}
[Authorize(Policy="Anulaciones")]
public sealed class RevisionModel(MegaDbContext db,CancellationService service):ManagementPage
{
    [BindProperty(SupportsGet=true)]public long Id{get;set;}[BindProperty]public long ExpectedVersion{get;set;}[BindProperty]public string ExpectedHash{get;set;}="";[BindProperty]public string? Reason{get;set;}
    public Autorizacion Review{get;private set;}=null!;public CancellationReview Data{get;private set;}=null!;public bool Current{get;private set;}
    public bool CanDecide=>Current&&Review.Decision=="pendiente"&&Review.SolicitanteId!=UserId&&(User.IsInRole("Administrador")||User.IsInRole("Propietario"));
    private async Task<bool> Load()
    {var a=await db.Set<Autorizacion>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==Id&&x.SolicitudAnulacionId!=null);if(a is null)return false;await service.RequireReadAsync(UserId,a.SolicitudAnulacionId!.Value);if(Formats.Hash(a.ContenidoSnapshot)!=a.ContenidoHash)throw new InvalidOperationException("La revisión requiere comprobar integridad.");Review=a;Data=JsonSerializer.Deserialize<CancellationReview>(a.ContenidoSnapshot,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;Current=a.InvalidadaEn==null&&Formats.Hash(Formats.Json(await service.ReviewAsync(a.SolicitudAnulacionId.Value)))==a.ContenidoHash;return true;}
    public async Task<IActionResult> OnGetAsync(){try{if(!await Load())return NotFound();}catch(BusinessException){return Forbid();}ExpectedVersion=Review.ObjetoVersion;ExpectedHash=Review.ContenidoHash;return Page();}
    public Task<IActionResult> OnPostApproveAsync()=>Decide(true);public Task<IActionResult> OnPostRejectAsync()=>Decide(false);
    private async Task<IActionResult> Decide(bool approve)
    {if(!User.IsInRole("Administrador")&&!User.IsInRole("Propietario"))return Forbid();try{if(ModelState.IsValid){await service.DecideAsync(UserId,Id,ExpectedVersion,ExpectedHash,approve,Reason??"");TempData["Message"]=approve?"Anulación autorizada. El efectivo sigue pendiente hasta registrar su entrega real.":"Solicitud rechazada sin efectos.";return RedirectToPage(new{id=Id});}}catch(BusinessException e){ModelState.AddModelError("",e.Message);}if(!await Load())return NotFound();return Page();}
}
