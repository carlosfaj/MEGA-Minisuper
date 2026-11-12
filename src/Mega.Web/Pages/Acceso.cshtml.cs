using System.Security.Claims;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace Mega.Web.Pages;
[AllowAnonymous,EnableRateLimiting("acceso")]
public sealed class AccesoModel(MegaDbContext db,IPasswordHasher<Usuario> hasher,Operations operations,IConfiguration config):PageModel
{
    [BindProperty] public string Account {get;set;}="";
    [BindProperty] public string Password {get;set;}="";
    public bool Demo=>config.GetValue<bool>("MEGA:DemoData");
    public IActionResult OnGet()=>User.Identity?.IsAuthenticated==true?RedirectToPage("/Index"):Page();
    public async Task<IActionResult> OnPostAsync()
    {
        if(!ModelState.IsValid || Account.Length>100 || Password.Length>200) return Page();
        var name=Account.Trim().ToUpperInvariant(); var user=await db.Set<Usuario>().SingleOrDefaultAsync(x=>x.NombreNormalizado==name);
        // A dummy hash keeps unknown usernames on the password verification path.
        var probe=user??new Usuario(); var hash=user?.PasswordHash??DummyHash;
        var result=hasher.VerifyHashedPassword(probe,hash,Password);
        if(user is null || user.Activo!=1 || user.TipoCuenta!="comercial" || result==PasswordVerificationResult.Failed)
        {
            if(user is not null) {operations.Audit(user.Id,"Acceso rechazado","Usuario",user.Id.ToString(),Guid.NewGuid().ToString(),new {motivo="Credenciales no válidas"},"rechazado");await db.SaveChangesAsync();}
            ModelState.AddModelError("","Usuario o contraseña no válidos."); return Page();
        }
        var role=await db.Set<Rol>().SingleAsync(x=>x.Id==user.RolId);
        if(result==PasswordVerificationResult.SuccessRehashNeeded) user.PasswordHash=hasher.HashPassword(user,Password);
        var claims=new[]{new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Name,user.NombreVisible),new Claim(ClaimTypes.Role,role.Nombre),
            new Claim("caja",user.PermisoOperativoCaja.ToString()),new Claim("version",user.CredencialesVersion.ToString())};
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));
        operations.Audit(user.Id,"Iniciar sesión","Usuario",user.Id.ToString(),Guid.NewGuid().ToString(),new {cuenta=user.NombreCuenta});await db.SaveChangesAsync();
        return RedirectToPage("/Index");
    }
    private static readonly string DummyHash=new PasswordHasher<Usuario>().HashPassword(new Usuario(),Guid.NewGuid().ToString());
}
