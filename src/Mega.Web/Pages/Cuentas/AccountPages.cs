using System.ComponentModel.DataAnnotations;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages.Cuentas;

[Authorize(Policy = "Cuentas")]
public sealed class IndexModel(MegaDbContext db) : ManagementPage
{
    public List<Usuario> Accounts { get; private set; } = [];
    public Dictionary<long, string> Roles { get; private set; } = [];
    public async Task OnGetAsync()
    {
        Accounts = await db.Set<Usuario>().AsNoTracking().Where(x => x.TipoCuenta == "comercial").OrderBy(x => x.NombreCuenta).ToListAsync();
        Roles = await db.Set<Rol>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Nombre);
    }
}
public sealed class AccountInput
{
    public long? Id { get; set; }
    public long Version { get; set; }
    [Required, StringLength(100)] public string Account { get; set; } = "";
    [Required(ErrorMessage = "Introduce el nombre visible."), StringLength(100)] public string Name { get; set; } = "";
    public long RoleId { get; set; } = 3;
    public bool Cash { get; set; }
    public bool Delegated { get; set; }
    public bool Active { get; set; } = true;
    [StringLength(200, MinimumLength = 12, ErrorMessage = "La contraseña debe tener entre 12 y 200 caracteres.")] public string? Password { get; set; }
}
[Authorize(Policy = "Cuentas")]
public sealed class EditarModel(MegaDbContext db, AccountService service) : ManagementPage
{
    [BindProperty] public AccountInput Input { get; set; } = new();
    public List<Rol> Roles { get; private set; } = [];
    public bool Owner => User.IsInRole("Propietario");
    private async Task LoadAsync() => Roles = await db.Set<Rol>().AsNoTracking().Where(x => Owner || x.Nombre != "Propietario").OrderBy(x => x.Nombre).ToListAsync();
    public async Task<IActionResult> OnGetAsync(long? id)
    {
        await LoadAsync();
        if (id is long key)
        {
            var u = await db.Set<Usuario>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == key && x.TipoCuenta == "comercial"); if (u is null) return NotFound();
            if (!Owner && (u.DelegacionCuentas == 1 || u.RolId == await db.Set<Rol>().Where(x => x.Nombre == "Propietario").Select(x => x.Id).SingleAsync())) return Forbid();
            Input = new() { Id = u.Id, Version = u.CredencialesVersion, Account = u.NombreCuenta, Name = u.NombreVisible, RoleId = u.RolId!.Value, Cash = u.PermisoOperativoCaja == 1, Delegated = u.DelegacionCuentas == 1, Active = u.Activo == 1 };
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (ModelState.IsValid)
            {
                await service.SaveAsync(UserId, new(Input.Id, Input.Version, Input.Account, Input.Name, Input.RoleId, Input.Cash, Input.Delegated, Input.Active, Input.Password));
                TempData["Message"] = "Cuenta guardada. Las sesiones anteriores de esa cuenta requieren un nuevo acceso."; return RedirectToPage("Index");
            }
        }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        // No devolver la contraseña en el HTML después de un error.
        ModelState.Remove("Input.Password"); Input.Password = null; await LoadAsync(); return Page();
    }
}
public sealed class SupportInput
{
    [Required, StringLength(100)] public string Account { get; set; } = "";
    [Required, StringLength(200, MinimumLength = 12)] public string Password { get; set; } = "";
    [Range(1, 24)] public int Hours { get; set; } = 1;
    [Required(ErrorMessage = "Explica el motivo del acceso."), StringLength(1000)] public string Reason { get; set; } = "";
}
[Authorize(Roles = "Propietario")]
public sealed class TemporalModel(MegaDbContext db, AccountService service) : ManagementPage
{
    [BindProperty] public SupportInput Input { get; set; } = new();
    [BindProperty] public long GrantId { get; set; }
    [BindProperty] public string? RevokeReason { get; set; }
    public List<AccesoSoporte> Grants { get; private set; } = [];
    public Dictionary<long, string> Names { get; private set; } = [];
    private async Task LoadAsync()
    {
        Grants = await db.Set<AccesoSoporte>().AsNoTracking().OrderByDescending(x => x.Id).Take(100).ToListAsync();
        Names = await db.Set<Usuario>().AsNoTracking().Where(x => x.TipoCuenta == "soporte_temporal").ToDictionaryAsync(x => x.Id, x => x.NombreCuenta);
    }
    public async Task OnGetAsync() => await LoadAsync();
    public async Task<IActionResult> OnPostAsync()
    {
        try { if (ModelState.IsValid) { await service.CreateSupportAsync(UserId, Input.Account, Input.Password, Input.Hours, Input.Reason); TempData["Message"] = "Acceso temporal autorizado. Entrega las credenciales por un medio adecuado."; return RedirectToPage(); } }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        ModelState.Remove("Input.Password"); Input.Password = ""; await LoadAsync(); return Page();
    }
    public async Task<IActionResult> OnPostRevokeAsync()
    {
        ModelState.Clear();
        try { await service.RevokeSupportAsync(UserId, GrantId, RevokeReason ?? ""); TempData["Message"] = "Acceso temporal revocado."; return RedirectToPage(); }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        await LoadAsync(); return Page();
    }
}
