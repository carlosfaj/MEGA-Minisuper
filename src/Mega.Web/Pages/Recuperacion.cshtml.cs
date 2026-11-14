using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Mega.Web.Pages;
public sealed class RecoveryInput
{public long BackupId{get;set;}public string Token{get;set;}="";public string Key{get;set;}=Guid.NewGuid().ToString();public string Password{get;set;}="";public string Reason{get;set;}="";public bool Acknowledged{get;set;}}
[Authorize(Policy="Propietario")]
public sealed class RecuperacionModel(MegaDbContext db,RecoveryService recovery,RecoveryGate gate):ManagementPage
{
    [BindProperty]public RecoveryInput Input{get;set;}=new();
    public List<RespaldoEjecucion> Backups{get;private set;}=[];public List<Restauracion> Rows{get;private set;}=[];public RecoveryPreview? Review{get;private set;}
    public RecoveryControl State=>gate.State;
    public static string Label(string table)=>table switch{"Venta"=>"Ventas","VentaDetalle"=>"Líneas de venta","Pago"=>"Cobros","MovimientoCaja"=>"Movimientos de caja","MovimientoInventario"=>"Movimientos de inventario","TurnoCaja"=>"Turnos de caja","SolicitudImpresion"=>"Trabajos de impresión","SolicitudCorreo"=>"Solicitudes de correo","EventoAuditoria"=>"Eventos de bitácora","OperacionInventario"=>"Operaciones de inventario","RespaldoEjecucion"=>"Ejecuciones de respaldo","ConfiguracionNegocio"=>"Versiones de configuración","Usuario"=>"Cuentas","Autorizacion"=>"Revisiones de autorización","Recepcion"=>"Recepciones","SolicitudInventario"=>"Solicitudes de inventario","SolicitudAnulacion"=>"Solicitudes de anulación",_=>System.Text.RegularExpressions.Regex.Replace(table,"(?<=[a-záéíóú])(?=[A-Z])"," ")};
    public IEnumerable<(string Label,long Before,long Recovered)> Comparison(Restauracion row)
    {
        using var json=JsonDocument.Parse(row.ConciliacionDetalle!);var before=json.RootElement.GetProperty("antes");var recovered=json.RootElement.GetProperty("recuperado");
        return before.EnumerateObject().Where(x=>x.Value.GetInt64()!=recovered.GetProperty(x.Name).GetInt64()||x.Name is "Venta" or "TurnoCaja" or "SolicitudImpresion" or "SolicitudCorreo").Select(x=>(Label(x.Name),x.Value.GetInt64(),recovered.GetProperty(x.Name).GetInt64())).ToList();
    }
    public async Task<IActionResult> OnGetPreservationAsync(long id)
    {try{var result=await recovery.DownloadPreservationAsync(UserId,id);return File(result.Stream,"application/zip",result.Name);}catch(BusinessException error){return BadRequest(error.Message);}catch(IOException){return BadRequest("La preservación no está disponible. Revisa el almacén local antes de continuar.");}}
    public async Task OnGetAsync(long? backup)
    {await LoadAsync();if(backup is long id)await ReviewAsync(id);}
    private async Task ReviewAsync(long id)
    {try{Review=await recovery.PreviewAsync(UserId,id);Input.BackupId=id;Input.Token=Review.Token;}catch(Exception error)when(error is BusinessException or IOException or UnauthorizedAccessException or InvalidDataException){ModelState.AddModelError("",error is BusinessException?error.Message:"No se pudo leer y comprobar la copia. Conecta el medio y revisa sus permisos.");}}
    public async Task<IActionResult> OnPostRestoreAsync()
    {
        try
        {
            if(ModelState.IsValid){await recovery.RestoreAsync(UserId,new(Input.BackupId,Input.Token,Input.Key,Input.Password,Input.Reason,Input.Acknowledged),HttpContext);await HttpContext.SignOutAsync();TempData["Message"]="Copia recuperada. Inicia sesión como Propietario para conciliar y reabrir.";return RedirectToPage("/Acceso");}
        }
        catch(Exception error)when(error is BusinessException or IOException or UnauthorizedAccessException or InvalidDataException){ModelState.AddModelError("",error is BusinessException?error.Message:"La copia no está disponible o no pudo comprobarse. Revisa el medio antes de continuar.");}
        Input.Password="";ModelState.Remove("Input.Password");if(gate.State.Key==Input.Key){Input.Key=Guid.NewGuid().ToString();ModelState.Remove("Input.Key");}if(!gate.DatabaseSafe)return new ContentResult{StatusCode=503,ContentType="text/html; charset=utf-8",Content="<h1>Operación pausada</h1><p>La intervención requiere revisión técnica.</p><a href='/Recuperacion/Estado'>Consultar estado</a>"};
        await LoadAsync();if(gate.State.Mode=="normal")await ReviewAsync(Input.BackupId);return Page();
    }
    public async Task<IActionResult> OnPostReopenAsync(long id,string? notes,bool inventory,bool cash,bool changes,bool queues)
    {
        ModelState.Clear();try{await recovery.ReopenAsync(UserId,id,notes??"",inventory,cash,changes,queues,HttpContext);TempData["Message"]="Núcleo reabierto. Impresión, correo y pauta siguen desactivados; los trabajos recuperados conservan su cuarentena.";return RedirectToPage();}
        catch(BusinessException error){ModelState.AddModelError("",error.Message);}await LoadAsync();return Page();
    }
    private async Task LoadAsync()
    {Backups=await db.Set<RespaldoEjecucion>().AsNoTracking().Where(x=>x.Estado=="Comprobada").OrderByDescending(x=>x.Id).Take(100).ToListAsync();Rows=await db.Set<Restauracion>().AsNoTracking().OrderByDescending(x=>x.Id).Take(50).ToListAsync();}
}
