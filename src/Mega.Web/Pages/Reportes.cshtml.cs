using System.Globalization;
using System.Security.Cryptography;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages;
[Authorize(Policy="Inventario")]
public sealed class ReportesModel(MegaDbContext db,ReportService service,Operations operations):ManagementPage
{
    [BindProperty(SupportsGet=true)]public string? Kind{get;set;}="inventario";
    [BindProperty(SupportsGet=true)]public string? From{get;set;}=DateTime.UtcNow.AddHours(-6).ToString("yyyy-MM-01",CultureInfo.InvariantCulture);
    [BindProperty(SupportsGet=true)]public string? To{get;set;}=DateTime.UtcNow.AddHours(-6).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
    [BindProperty(SupportsGet=true)]public long? ProductId{get;set;}
    [BindProperty(SupportsGet=true)]public long? OperatorId{get;set;}
    public bool Manager=>User.IsInRole("Administrador")||User.IsInRole("Propietario");
    public ReportSnapshot? Report{get;private set;}
    public Dictionary<long,string> Products{get;private set;}=[];
    public Dictionary<long,string> Operators{get;private set;}=[];
    private ReportFilter Filter=>new(Kind??"",From??"",To??"",ProductId,OperatorId);
    public async Task OnGetAsync()
    {try{if(ModelState.IsValid)Report=await service.RunAsync(UserId,Filter);}catch(BusinessException e){ModelState.AddModelError("",e.Message);}await LoadAsync();}
    public async Task<IActionResult> OnGetExportAsync(string format)
    {
        if(!ModelState.IsValid)return BadRequest("Revisa los filtros del reporte.");
        try
        {
            if(format is not ("pdf" or "xlsx"))return BadRequest("Selecciona PDF o Excel.");var report=await service.RunAsync(UserId,Filter);var bytes=format=="pdf"?ReportExport.Pdf(report):ReportExport.Excel(report);
            var hash=Convert.ToHexStringLower(SHA256.HashData(bytes));operations.Audit(UserId,"Exportar reporte","Reporte",Kind,Guid.NewGuid().ToString(),new{filtros=Filter,formato=format,hashArchivo=hash,report.GeneratedAt});await db.SaveChangesAsync();
            return File(bytes,format=="pdf"?"application/pdf":"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",$"MEGA-{Kind}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.{format}");
        }
        catch(BusinessException e){return BadRequest(e.Message);}
    }
    private async Task LoadAsync()
    {Products=await db.Set<Producto>().OrderBy(x=>x.Nombre).ToDictionaryAsync(x=>x.Id,x=>x.Nombre);if(Manager)Operators=await db.Set<Usuario>().Where(x=>x.TipoCuenta=="comercial").OrderBy(x=>x.NombreCuenta).ToDictionaryAsync(x=>x.Id,x=>x.NombreCuenta);}
}
