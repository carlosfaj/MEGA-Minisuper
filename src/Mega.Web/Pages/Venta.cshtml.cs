using System.Security.Claims;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Mega.Web.Pages;
[Authorize(Policy="Caja")]
public sealed class VentaModel(Operations operations):PageModel
{
    public bool HasShift {get;private set;}
    private long UserId=>long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public long AccountId=>UserId;
    public async Task OnGetAsync()=>HasShift=(await operations.CashAsync(UserId)).Shift is not null;
    public async Task<IActionResult> OnGetProductosAsync()=>new JsonResult(await operations.ProductsAsync());
    public async Task<IActionResult> OnPostCotizarAsync([FromBody] List<LineInput>? lines)
    {
        if(!ModelState.IsValid)return new JsonResult(new {error="Revisa los datos de la compra."}){StatusCode=400};
        try{return new JsonResult(await operations.QuoteAsync(UserId,lines));}
        catch(BusinessException e){return new JsonResult(new {error=e.Message}){StatusCode=400};}
        catch(OverflowException){return new JsonResult(new {error="La cantidad o total supera la capacidad de registro."}){StatusCode=400};}
    }
    public async Task<IActionResult> OnPostConfirmarAsync([FromBody] SaleInput input)
    {
        if(!ModelState.IsValid)return new JsonResult(new {error="Revisa los datos del cobro."}){StatusCode=400};
        try{var id=await operations.ConfirmAsync(UserId,input);return new JsonResult(new {id,url=Url.Page("/Comprobante",new {id})});}
        catch(BusinessException e){return new JsonResult(new {error=e.Message}){StatusCode=400};}
        catch(OverflowException){return new JsonResult(new {error="El importe supera la capacidad de registro."}){StatusCode=400};}
    }
}
