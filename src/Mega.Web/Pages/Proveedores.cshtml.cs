using System.ComponentModel.DataAnnotations;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages;

public sealed class SupplierInput
{
    public long? Id { get; set; }
    public long Version { get; set; }
    [Required(ErrorMessage = "Introduce el nombre del proveedor."), StringLength(200)] public string Name { get; set; } = "";
    [StringLength(100)] public string? Identification { get; set; }
    [StringLength(100)] public string? Phone { get; set; }
    [StringLength(500)] public string? Address { get; set; }
    [StringLength(200)] public string? Contact { get; set; }
    public bool Active { get; set; } = true;
}
[Authorize(Policy = "Inventario")]
public sealed class ProveedoresModel(MegaDbContext db, ReceivingService service) : ManagementPage
{
    [BindProperty] public SupplierInput Input { get; set; } = new();
    public List<Proveedor> Suppliers { get; private set; } = [];
    public bool CanEdit => User.IsInRole("Administrador") || User.IsInRole("Propietario");
    public async Task OnGetAsync(long? id)
    {
        Suppliers = await db.Set<Proveedor>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync();
        if (id is long key && CanEdit && Suppliers.SingleOrDefault(x => x.Id == key) is { } p)
            Input = new() { Id = p.Id, Version = p.Version, Name = p.Nombre, Identification = p.Identificacion, Phone = p.Telefono, Address = p.Direccion, Contact = p.Contacto, Active = p.Activo == 1 };
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!CanEdit) return Forbid();
        try { if (ModelState.IsValid) { await service.SaveSupplierAsync(UserId, new(Input.Id, Input.Version, Input.Name, Input.Identification, Input.Phone, Input.Address, Input.Contact, Input.Active)); TempData["Message"] = "Proveedor guardado."; return RedirectToPage(); } }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        Suppliers = await db.Set<Proveedor>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(); return Page();
    }
}
