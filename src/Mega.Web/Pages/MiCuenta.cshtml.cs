using System.ComponentModel.DataAnnotations;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mega.Web.Pages;

[Authorize(Policy = "Comercial")]
public sealed class MiCuentaModel(AccountService service) : ManagementPage
{
    [BindProperty, Required, StringLength(200)] public string CurrentPassword { get; set; } = "";
    [BindProperty, Required, StringLength(200, MinimumLength = 12)] public string NewPassword { get; set; } = "";
    [BindProperty, Required, Compare(nameof(NewPassword), ErrorMessage = "Las contraseñas nuevas no coinciden.")] public string Confirmation { get; set; } = "";
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (ModelState.IsValid)
            {
                await service.ChangePasswordAsync(UserId, CurrentPassword, NewPassword);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return RedirectToPage("/Acceso");
            }
        }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        return Page();
    }
}
