using System.Security.Claims;
using Mega.Web.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Mega.Web.Pages;
public sealed class IndexModel(Operations operations):PageModel
{
    public CashSummary? Cash {get;private set;}
    public async Task OnGetAsync(){if(User.HasClaim("caja","1")) Cash=await operations.CashAsync(long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));}
}
