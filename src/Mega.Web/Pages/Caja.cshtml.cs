using System.Security.Claims;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Mega.Web.Pages;
[Authorize(Policy="Caja")]
public sealed class CajaModel(Operations operations):PageModel
{
    public CashSummary Cash {get;private set;}=null!;
    [BindProperty(SupportsGet=true)] public bool Review {get;set;}
    [BindProperty] public string? Fund {get;set;}
    [BindProperty] public string? Counted {get;set;}
    [BindProperty] public string? Reason {get;set;}
    [BindProperty] public long ShiftId {get;set;}
    [BindProperty] public long Version {get;set;}
    private long UserId=>long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task OnGetAsync()=>Cash=await operations.CashAsync(UserId);
    public async Task<IActionResult> OnPostAbrirAsync()
    {
        try{if(ModelState.IsValid){await operations.OpenCashAsync(UserId,Fund??"",Reason);TempData["Message"]="Tu turno está abierto. Ya puedes registrar ventas.";return RedirectToPage();}}
        catch(BusinessException e){ModelState.AddModelError("",e.Message);}
        Cash=await operations.CashAsync(UserId);return Page();
    }
    public async Task<IActionResult> OnPostCerrarAsync()
    {
        try{if(ModelState.IsValid){await operations.CloseCashAsync(UserId,ShiftId,Version,Counted??"",Reason);TempData["Message"]="Turno cerrado. El conteo y la diferencia quedaron registrados.";return RedirectToPage();}}
        catch(BusinessException e){ModelState.AddModelError("",e.Message);}
        Review=true;Cash=await operations.CashAsync(UserId);return Page();
    }
}
