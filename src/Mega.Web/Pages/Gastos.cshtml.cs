using System.Globalization;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages;
[Authorize(Policy="Gestion")]
public sealed class GastosModel(MegaDbContext db,ExpenseService service):ManagementPage
{
    [BindProperty]public ExpenseChange Input{get;set;}=new(Guid.NewGuid().ToString(),DateTime.UtcNow.AddHours(-6).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),"","",null);
    public List<GastoOperativo> Rows{get;private set;}=[];
    public async Task<IActionResult> OnGetAsync(long? corrects)
    {
        if(corrects is long id)
        {var old=await db.Set<GastoOperativo>().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id&&x.Estado=="Registrado");if(old is null)return NotFound();Input=new(Guid.NewGuid().ToString(),DateTimeOffset.Parse(old.Fecha).AddHours(-6).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),old.Concepto,(old.ImporteCentavos/100m).ToString("0.00",CultureInfo.InvariantCulture),old.ComprobanteReferencia,id,null,ExpenseService.ReviewHash(old));}
        await LoadAsync();return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {try{if(ModelState.IsValid){await service.SaveAsync(UserId,Input);TempData["Message"]="Gasto registrado con su historia conservada.";return RedirectToPage();}}catch(BusinessException e){ModelState.AddModelError("",e.Message);}await LoadAsync();return Page();}
    public async Task<IActionResult> OnPostCancelAsync(long id,string hash,string reason)
    {ModelState.Clear();try{await service.CancelAsync(UserId,id,hash,reason);TempData["Message"]="Gasto anulado con motivo; el registro original se conserva.";return RedirectToPage();}catch(BusinessException e){ModelState.AddModelError("",e.Message);}await LoadAsync();return Page();}
    private async Task LoadAsync()=>Rows=await db.Set<GastoOperativo>().AsNoTracking().OrderByDescending(x=>x.Id).Take(100).ToListAsync();
}
