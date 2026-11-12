using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages;
public sealed class MailSettingsInput
{
    public long Id{get;set;}public long Version{get;set;}public bool Enabled{get;set;}public string? Host{get;set;}public int Port{get;set;}=587;public string? Security{get;set;}="starttls";public string? Sender{get;set;}public string? User{get;set;}public string? Password{get;set;}public bool ClearPassword{get;set;}
}
[Authorize(Policy="Gestion")]
public sealed class CorreosModel(MegaDbContext db,SettingsService settings,MailConfiguration configuration,MailQueue queue):ManagementPage
{
    [BindProperty]public MailSettingsInput Input{get;set;}=new();
    public bool HasSecret{get;private set;}
    public List<SolicitudCorreo> Jobs{get;private set;}=[];public List<IntentoCorreo> Attempts{get;private set;}=[];
    public async Task OnGetAsync()=>await LoadAsync(true);
    public async Task<IActionResult> OnPostSettingsAsync()
    {
        try{if(ModelState.IsValid){await configuration.SaveAsync(UserId,new(Input.Id,Input.Version,Input.Enabled,Input.Host,Input.Port,Input.Security??"",Input.Sender,Input.User,Input.Password,Input.ClearPassword));TempData["Message"]="Configuración de correo guardada. La credencial no se muestra ni se guarda en la base como texto.";return RedirectToPage();}}catch(BusinessException e){ModelState.AddModelError("",e.Message);}
        Input.Password=null;ModelState.Remove("Input.Password");await LoadAsync(false);return Page();
    }
    public async Task<IActionResult> OnPostResolveAsync(long id,string hash,string resolution,string reason,bool checkedServer)
    {
        ModelState.Clear();try{await queue.ResolveAsync(UserId,id,hash,resolution,reason,checkedServer);TempData["Message"]="Resolución del correo registrada.";return RedirectToPage();}catch(BusinessException e){ModelState.AddModelError("",e.Message);}await LoadAsync(true);return Page();
    }
    public async Task<IActionResult> OnGetDownloadAsync(long id)
    {try{var bytes=await queue.DownloadAsync(UserId,id);var row=await db.Set<SolicitudCorreo>().AsNoTracking().SingleAsync(x=>x.Id==id);var extension=Path.GetExtension(row.ReporteReferencia);return File(bytes,extension==".pdf"?"application/pdf":"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet","MEGA-reporte-conservado-"+id+extension);}catch(BusinessException e){return BadRequest(e.Message);}}
    private async Task LoadAsync(bool reset)
    {
        var c=await settings.CurrentAsync();HasSecret=c.SmtpSecretoReferencia is not null;
        if(reset)Input=new(){Id=c.Id,Version=c.Version,Enabled=c.SmtpHost is not null,Host=c.SmtpHost,Port=checked((int)(c.SmtpPuerto??587)),Security=c.SmtpSeguridad??"starttls",Sender=c.CorreoRemitente,User=c.SmtpUsuario};
        Jobs=await db.Set<SolicitudCorreo>().AsNoTracking().OrderByDescending(x=>x.Id).Take(100).ToListAsync();var ids=Jobs.Select(x=>x.Id).ToArray();Attempts=await db.Set<IntentoCorreo>().AsNoTracking().Where(x=>ids.Contains(x.SolicitudCorreoId)).OrderBy(x=>x.Numero).ToListAsync();
    }
}
