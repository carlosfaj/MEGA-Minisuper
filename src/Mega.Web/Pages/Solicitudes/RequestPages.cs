using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages.Solicitudes;

[Authorize(Policy="Inventario")]
public sealed class IndexModel(MegaDbContext db):ManagementPage
{
    public List<SolicitudInventario> Requests {get;private set;}=[];
    public Dictionary<long,long> Pending {get;private set;}=[];
    public async Task OnGetAsync()
    {
        Requests=await db.Set<SolicitudInventario>().AsNoTracking().OrderByDescending(x=>x.Id).Take(200).ToListAsync();
        Pending=await db.Set<Autorizacion>().AsNoTracking().Where(x=>x.SolicitudInventarioId!=null&&x.Decision=="pendiente"&&x.InvalidadaEn==null).ToDictionaryAsync(x=>x.SolicitudInventarioId!.Value,x=>x.Id);
    }
}
public sealed class RequestInput
{
    public long? Id {get;set;} public long Version {get;set;}
    public string Key {get;set;}=Guid.NewGuid().ToString();
    public string Type {get;set;}="carga";
    public long? CountId {get;set;}
    [Required(ErrorMessage="Introduce una referencia única."),StringLength(100)] public string Reference {get;set;}="";
    [Required(ErrorMessage="Explica el motivo."),StringLength(1000)] public string Reason {get;set;}="";
    [Required(ErrorMessage="Indica dónde se conserva la evidencia."),StringLength(1000)] public string Evidence {get;set;}="";
    public List<RequestLineInput> Lines {get;set;}=[new()];
}
public sealed class RequestLineInput
{
    public long ProductId {get;set;} public string? Quantity {get;set;} public string? BaseCost {get;set;} public string? Cause {get;set;} public long? ReturnSupplierId {get;set;}
    [StringLength(1000)] public string? Evidence {get;set;}
    public long? LotId {get;set;}
    [StringLength(100)] public string? NewLot {get;set;}
    public string? Expiry {get;set;}
    public bool Empty=>ProductId==0&&LotId==null&&string.IsNullOrWhiteSpace(NewLot)&&string.IsNullOrWhiteSpace(Expiry)&&string.IsNullOrWhiteSpace(Quantity)&&string.IsNullOrWhiteSpace(BaseCost)&&string.IsNullOrWhiteSpace(Cause)&&ReturnSupplierId==null&&string.IsNullOrWhiteSpace(Evidence);
}
[Authorize(Policy="Inventario")]
public sealed class EditarModel(MegaDbContext db,InventoryRequestService service):ManagementPage
{
    [BindProperty] public RequestInput Input {get;set;}=new();
    [BindProperty] public string? CancelReason {get;set;}
    public SolicitudInventario? Saved {get;private set;}
    public List<Producto> Products {get;private set;}=[];
    public List<Proveedor> Suppliers {get;private set;}=[];
    public List<LoteExistencia> Lots {get;private set;}=[];
    public List<Conteo> Counts {get;private set;}=[];
    public List<Autorizacion> Reviews {get;private set;}=[];
    public bool Editable=>Saved is null||Saved.SolicitadoPor==UserId&&Saved.Estado is not("Autorizada" or "Cancelada");
    public string TypeName=>Input.Type=="ajuste"?"Ajuste desde conteo":Input.Type=="salida_justificada"?"Salida justificada":"Carga de inventario";
    private async Task LoadAsync()
    {
        Products=await db.Set<Producto>().AsNoTracking().OrderBy(x=>x.Nombre).ToListAsync();Suppliers=await db.Set<Proveedor>().AsNoTracking().OrderBy(x=>x.Nombre).ToListAsync();Counts=await db.Set<Conteo>().AsNoTracking().Where(x=>x.Estado=="Finalizado").OrderByDescending(x=>x.Id).ToListAsync();
        Lots=await db.Set<LoteExistencia>().AsNoTracking().OrderBy(x=>x.ProductoId).ThenBy(x=>x.Id).ToListAsync();
        if(Input.Id is long id){Saved=await db.Set<SolicitudInventario>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id);Reviews=await db.Set<Autorizacion>().AsNoTracking().Where(x=>x.SolicitudInventarioId==id).OrderByDescending(x=>x.Id).ToListAsync();}
    }
    public async Task<IActionResult> OnGetAsync(long? id,string? type,long? countId)
    {
        if(id is long key)
        {
            var r=await db.Set<SolicitudInventario>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==key);if(r is null)return NotFound();
            Input=new(){Id=r.Id,Version=r.Version,Key=r.ClaveSolicitud,Type=r.Tipo,CountId=r.ConteoId,Reference=r.Referencia,Reason=r.Motivo??"",Evidence=r.EvidenciaReferencia??"",Lines=[]};
            foreach(var d in await db.Set<SolicitudInventarioDetalle>().AsNoTracking().Where(x=>x.SolicitudId==key).OrderBy(x=>x.NumeroLinea).ToListAsync())
                Input.Lines.Add(new(){ProductId=d.ProductoId,LotId=d.LoteId,Quantity=((r.Tipo=="salida_justificada"?-d.CantidadEscalada:d.CantidadEscalada)/ReceivingService.Power(d.Escala)).ToString("0.######",CultureInfo.InvariantCulture),BaseCost=d.CostoUnitEscalado is long cost?(cost/ReceivingService.Power(d.EscalaCosto!.Value)).ToString("0.######",CultureInfo.InvariantCulture):null,Cause=d.Causa,ReturnSupplierId=d.ProveedorDevolucionId,Evidence=d.EvidenciaReferencia});
        }
        else {Input.Type=type is "ajuste" or "salida_justificada"?type:"carga";Input.CountId=Input.Type=="ajuste"?countId:null;}
        await LoadAsync();return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if(ModelState.IsValid)
            {
                var lines=Input.Type=="ajuste"?new List<InventoryLine>():Input.Lines.Where(x=>!x.Empty).Select(x=>new InventoryLine(x.ProductId,x.Quantity??"",x.BaseCost,x.Cause,x.ReturnSupplierId,x.Evidence,x.LotId,x.NewLot,x.Expiry)).ToList();
                var id=await service.SaveAsync(UserId,new(Input.Id,Input.Version,Input.Key,Input.Reference,Input.Type,Input.CountId,Input.Reason,Input.Evidence,lines));TempData["Message"]="Solicitud guardada sin modificar existencias. Envía la versión guardada para revisión.";return RedirectToPage(new{id});
            }
        }
        catch(BusinessException e){ModelState.AddModelError("",e.Message);}await LoadAsync();return Page();
    }
    public async Task<IActionResult> OnPostAddLineAsync(){ModelState.Clear();if(Input.Lines.Count<100)Input.Lines.Add(new());await LoadAsync();return Page();}
    public async Task<IActionResult> OnPostSubmitAsync()
    {
        var id=Input.Id;ModelState.Clear();
        try{await service.SubmitAsync(UserId,id??0,Input.Version);TempData["Message"]="Solicitud enviada. Debe decidir otra cuenta de gestión.";return RedirectToPage(new{id});}
        catch(BusinessException e){await OnGetAsync(id,null,null);ModelState.Clear();ModelState.AddModelError("",e.Message);return Page();}
    }
    public async Task<IActionResult> OnPostCancelAsync()
    {
        var id=Input.Id;ModelState.Clear();
        try{await service.CancelAsync(UserId,id??0,Input.Version,CancelReason??"");TempData["Message"]="Solicitud cancelada sin modificar existencias.";return RedirectToPage(new{id});}
        catch(BusinessException e){await OnGetAsync(id,null,null);ModelState.Clear();ModelState.AddModelError("",e.Message);return Page();}
    }
}
[Authorize(Policy="Inventario")]
public sealed class RevisionModel(MegaDbContext db,InventoryRequestService service):ManagementPage
{
    [BindProperty(SupportsGet=true)] public long Id {get;set;}
    [BindProperty] public long ExpectedVersion {get;set;}
    [BindProperty] public string ExpectedHash {get;set;}="";
    [BindProperty] public string? Reason {get;set;}
    public Autorizacion Review {get;private set;}=null!;
    public InventoryReview Reviewed {get;private set;}=null!;
    public bool Current {get;private set;}
    public bool Legacy {get;private set;}
    public bool CanDecide=>!Legacy&&Current&&Review.Decision=="pendiente"&&Review.SolicitanteId!=UserId&&(User.IsInRole("Administrador")||User.IsInRole("Propietario"));
    private async Task<bool> LoadAsync()
    {
        var a=await db.Set<Autorizacion>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==Id&&x.SolicitudInventarioId!=null);if(a is null)return false;
        if(Formats.Hash(a.ContenidoSnapshot)!=a.ContenidoHash)throw new InvalidOperationException("La revisión requiere comprobar integridad.");Review=a;
        using var json=JsonDocument.Parse(a.ContenidoSnapshot);Legacy=!json.RootElement.TryGetProperty("requestId",out _);
        Reviewed=Legacy?await service.ReviewAsync(a.SolicitudInventarioId!.Value):JsonSerializer.Deserialize<InventoryReview>(a.ContenidoSnapshot,new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Current=!Legacy&&a.InvalidadaEn is null&&Formats.Hash(Formats.Json(await service.ReviewAsync(a.SolicitudInventarioId!.Value)))==a.ContenidoHash;return true;
    }
    public async Task<IActionResult> OnGetAsync(){if(!await LoadAsync())return NotFound();ExpectedVersion=Review.ObjetoVersion;ExpectedHash=Review.ContenidoHash;return Page();}
    public Task<IActionResult> OnPostApproveAsync()=>Decide(true);
    public Task<IActionResult> OnPostRejectAsync()=>Decide(false);
    private async Task<IActionResult> Decide(bool approve)
    {
        if(!User.IsInRole("Administrador")&&!User.IsInRole("Propietario"))return Forbid();
        try{if(ModelState.IsValid){await service.DecideAsync(UserId,Id,ExpectedVersion,ExpectedHash,approve,Reason??"");TempData["Message"]=approve?"Solicitud autorizada y aplicada una sola vez.":"Solicitud rechazada sin modificar existencias.";return RedirectToPage(new{id=Id});}}
        catch(BusinessException e){ModelState.AddModelError("",e.Message);}if(!await LoadAsync())return NotFound();return Page();
    }
}
