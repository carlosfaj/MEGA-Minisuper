using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages;
public sealed class BackupPolicyInput
{public long Id{get;set;}public long Version{get;set;}public string? Destination{get;set;}public string? Medium{get;set;}public bool Periodic{get;set;}public int Minutes{get;set;}=30;public bool OnClose{get;set;}public bool BeforeUpdate{get;set;}}
[Authorize(Policy="Gestion")]
public sealed class RespaldosModel(MegaDbContext db,SettingsService settings,BackupService service,BackupDispatcher dispatcher):ManagementPage
{
    [BindProperty]public BackupPolicyInput Input{get;set;}=new();
    public BackupPolicy? Policy{get;private set;}public List<RespaldoEjecucion> Rows{get;private set;}=[];
    public bool Owner=>User.IsInRole("Propietario");
    public async Task OnGetAsync()=>await LoadAsync(true);
    public async Task<IActionResult> OnPostPolicyAsync()
    {try{if(ModelState.IsValid){await service.SavePolicyAsync(UserId,new(Input.Id,Input.Version,Input.Destination??"",Input.Medium??"",Input.Periodic,Input.Minutes,Input.OnClose,Input.BeforeUpdate));TempData["Message"]="Destino y pauta autorizados por el Propietario.";return RedirectToPage();}}catch(BusinessException e){ModelState.AddModelError("",e.Message);}await LoadAsync(false);return Page();}
    public async Task<IActionResult> OnPostRunAsync(string key)
    {ModelState.Clear();try{await dispatcher.ManualAsync(UserId,key);TempData["Message"]="Ejecución registrada. Revisa su resultado antes de considerar la copia disponible.";return RedirectToPage();}catch(BusinessException e){ModelState.AddModelError("",e.Message);}await LoadAsync(true);return Page();}
    public async Task<IActionResult> OnPostVerifyAsync(long id)
    {ModelState.Clear();try{var valid=await service.VerifyAsync(UserId,id);TempData["Message"]=valid?"Copia comprobada: huella, archivos y base íntegros.":"La copia no pudo comprobarse ahora. Revisa el detalle de la ejecución.";return RedirectToPage();}catch(BusinessException e){ModelState.AddModelError("",e.Message);}await LoadAsync(true);return Page();}
    public async Task<IActionResult> OnGetDownloadAsync(long id)
    {try{var result=await service.DownloadAsync(UserId,id);return File(result.Stream,"application/zip",result.Name);}catch(BusinessException e){return BadRequest(e.Message);}catch(IOException){return BadRequest("El medio o archivo no están disponibles. Revisa la copia antes de descargar.");}}
    private async Task LoadAsync(bool reset)
    {
        var c=await settings.CurrentAsync();try{Policy=BackupService.Policy(c);}catch(BusinessException e){ModelState.AddModelError("",e.Message);}
        if(reset)Input=new(){Id=c.Id,Version=c.Version,Destination=Policy?.Destination,Medium=Policy?.Medium,Periodic=Policy?.Periodic??false,Minutes=Policy?.Minutes??30,OnClose=Policy?.OnClose??false,BeforeUpdate=Policy?.BeforeUpdate??false};
        Rows=await db.Set<RespaldoEjecucion>().AsNoTracking().OrderByDescending(x=>x.Id).Take(100).ToListAsync();
    }
}
