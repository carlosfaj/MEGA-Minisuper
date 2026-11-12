using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages.Comercial;
[Authorize(Policy="Gestion")]
public sealed class IndexModel:ManagementPage {public void OnGet(){} }
public sealed class PromotionInput
{
    public long? Id{get;set;}public long Version{get;set;}
    [Required,StringLength(200)]public string Name{get;set;}="";
    public string Type{get;set;}="porcentaje";
    [Required]public string Value{get;set;}="";
    [Required]public string Start{get;set;}=CommercialService.Local(Formats.Now());
    [Required]public string End{get;set;}=DateTime.UtcNow.AddHours(-6).AddDays(7).ToString("yyyy-MM-ddTHH:mm:ss",CultureInfo.InvariantCulture);
    public bool Active{get;set;}=true;public long Priority{get;set;}public List<long> Presentations{get;set;}=[];
}
[Authorize(Policy="Gestion")]
public sealed class PromocionesModel(MegaDbContext db,CommercialService service):ManagementPage
{
    [BindProperty]public PromotionInput Input{get;set;}=new();public List<Promocion> Rows{get;private set;}=[];public List<(long Id,string Name)> Options{get;private set;}=[];
    private async Task Load()
    {Rows=await db.Set<Promocion>().AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync();var products=await db.Set<Producto>().AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.Nombre);Options=(await db.Set<Presentacion>().AsNoTracking().OrderBy(x=>x.ProductoId).ToListAsync()).Select(x=>(x.Id,products[x.ProductoId]+" · "+x.Nombre)).ToList();}
    public async Task<IActionResult> OnGetAsync(long? id)
    {await Load();if(id is long key){var p=Rows.SingleOrDefault(x=>x.Id==key);if(p is null)return NotFound();var links=await db.Set<PromocionProducto>().AsNoTracking().Where(x=>x.PromocionId==key).ToListAsync();Input=new(){Id=p.Id,Version=p.Version,Name=p.Nombre,Type=p.Tipo,Value=(p.ValorEscalado/ReceivingService.Power(p.EscalaValor)).ToString("0.######",CultureInfo.InvariantCulture),Start=CommercialService.Local(p.Inicio),End=CommercialService.Local(p.Fin),Active=p.Activa==1,Priority=links.FirstOrDefault()?.Prioridad??0,Presentations=links.Select(x=>x.PresentacionId).ToList()};}return Page();}
    public async Task<IActionResult> OnPostAsync()
    {try{if(ModelState.IsValid){await service.SavePromotionAsync(UserId,new(Input.Id,Input.Version,Input.Name,Input.Type,Input.Value,Input.Start,Input.End,Input.Active,Input.Priority,Input.Presentations));TempData["Message"]="Promoción guardada. Las ventas anteriores conservan su cálculo.";return RedirectToPage();}}catch(BusinessException e){ModelState.AddModelError("",e.Message);}await Load();return Page();}
}
public sealed class TaxInput
{
    public long? Id{get;set;}public string? Hash{get;set;}
    [Required,StringLength(200)]public string Name{get;set;}="";
    [Required]public string Rate{get;set;}="";
    [Required]public string Start{get;set;}=CommercialService.Local(Formats.Now());
    public string? End{get;set;}public bool Exempt{get;set;}
    [StringLength(1000)]public string? ExemptionReason{get;set;}
    public bool Active{get;set;}=true;public List<long> Products{get;set;}=[];
}
[Authorize(Policy="Gestion")]
public sealed class ImpuestosModel(MegaDbContext db,CommercialService service):ManagementPage
{
    [BindProperty]public TaxInput Input{get;set;}=new();public List<Impuesto> Rows{get;private set;}=[];public List<Producto> Products{get;private set;}=[];
    private async Task Load(){Rows=await db.Set<Impuesto>().AsNoTracking().OrderByDescending(x=>x.Id).ToListAsync();Products=await db.Set<Producto>().AsNoTracking().OrderBy(x=>x.Nombre).ToListAsync();}
    public async Task<IActionResult> OnGetAsync(long? id)
    {await Load();if(id is long key){var t=Rows.SingleOrDefault(x=>x.Id==key);if(t is null)return NotFound();var links=await db.Set<ProductoImpuesto>().AsNoTracking().Where(x=>x.ImpuestoId==key).ToListAsync();Input=new(){Id=t.Id,Hash=CommercialService.TaxHash(t,links),Name=t.Nombre,Rate=(t.TasaEscalada/ReceivingService.Power(t.EscalaTasa)).ToString("0.######",CultureInfo.InvariantCulture),Start=CommercialService.Local(t.VigenteDesde),End=t.VigenteHasta is null?null:CommercialService.Local(t.VigenteHasta),Exempt=t.EsExencion==1,ExemptionReason=t.MotivoExencion,Active=t.Activo==1,Products=links.Select(x=>x.ProductoId).Distinct().ToList()};}return Page();}
    public async Task<IActionResult> OnPostAsync()
    {try{if(ModelState.IsValid){await service.SaveTaxAsync(UserId,new(Input.Id,Input.Hash,Input.Name,Input.Rate,Input.Start,Input.End,Input.Exempt,Input.ExemptionReason,Input.Active,Input.Products));TempData["Message"]="Condición guardada. No acredita validación fiscal y conserva ventas anteriores.";return RedirectToPage();}}catch(BusinessException e){ModelState.AddModelError("",e.Message);}await Load();return Page();}
}
