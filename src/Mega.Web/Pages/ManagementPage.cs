using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mega.Web.Pages;

public abstract class ManagementPage : PageModel
{
    protected long UserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
