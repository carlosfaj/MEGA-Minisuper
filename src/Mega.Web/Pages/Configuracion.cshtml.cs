using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages;

public sealed class SettingsInput
{
    public long Id { get; set; }
    public long Version { get; set; }
    [Required(ErrorMessage = "Introduce el nombre del negocio."), StringLength(200)] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Introduce la dirección."), StringLength(500)] public string Address { get; set; } = "";
    [StringLength(100)] public string? Phone { get; set; }
    [StringLength(200)] public string? Issuer { get; set; }
    [StringLength(100)] public string? IssuerId { get; set; }
    [StringLength(200)] public string? FiscalRegime { get; set; }
    [Required] public string UsualFund { get; set; } = "0";
    public long? ExpirationDays { get; set; }
    [StringLength(200)] public string? Printer { get; set; }
    public bool EnablePrint{get;set;}
}

[Authorize(Policy = "Gestion")]
public sealed class ConfiguracionModel(MegaDbContext db, SettingsService service) : ManagementPage
{
    [BindProperty] public SettingsInput Input { get; set; } = new();
    public List<ConfiguracionNegocio> History { get; private set; } = [];
    public async Task OnGetAsync()
    {
        var c = await service.CurrentAsync();
        Input = new() { Id = c.Id, Version = c.Version, Name = c.Nombre, Address = c.Direccion, Phone = c.Telefono,
            Issuer = c.Emisor, IssuerId = c.IdentificacionEmisor, FiscalRegime = c.RegimenFiscal,
            UsualFund = (c.FondoHabitualCentavos / 100m).ToString("0.00", CultureInfo.InvariantCulture), ExpirationDays = c.DiasAlertaVencimiento, Printer = c.ImpresoraNombre,EnablePrint=c.ImpresionHabilitada==1 };
        await LoadHistoryAsync();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (ModelState.IsValid)
            {
                await service.SaveAsync(UserId, new(Input.Id, Input.Version, Input.Name, Input.Address, Input.Phone, Input.Issuer,
                    Input.IssuerId, Input.FiscalRegime, Input.UsualFund, Input.ExpirationDays, Input.Printer,Input.EnablePrint));
                TempData["Message"] = "Configuración actualizada. Las ventas y turnos anteriores conservan sus condiciones.";
                return RedirectToPage();
            }
        }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        await LoadHistoryAsync(); return Page();
    }
    private async Task LoadHistoryAsync() => History = await db.Set<ConfiguracionNegocio>().AsNoTracking().OrderByDescending(x => x.Version).Take(30).ToListAsync();
}
