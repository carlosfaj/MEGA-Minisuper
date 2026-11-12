using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages.Conteos;

[Authorize(Policy = "Inventario")]
public sealed class IndexModel(MegaDbContext db) : ManagementPage
{
    public List<Conteo> Counts { get; private set; } = [];
    public async Task OnGetAsync() => Counts = await db.Set<Conteo>().AsNoTracking().OrderByDescending(x => x.Id).Take(200).ToListAsync();
}
public sealed class CountInput
{
    public long? Id { get; set; }
    public long Version { get; set; }
    [Required(ErrorMessage = "Introduce una referencia única."), StringLength(100)] public string Reference { get; set; } = "";
    [StringLength(2000)] public string? Notes { get; set; }
    public bool PhysicalChecked { get; set; }
    public List<CountLineInput> Lines { get; set; } = [new()];
}
public sealed class CountLineInput
{
    public long ProductId { get; set; }
    public long? LotId { get; set; }
    public string Physical { get; set; } = "";
    [StringLength(1000)] public string? Note { get; set; }
}
[Authorize(Policy = "Inventario")]
public sealed class EditarModel(MegaDbContext db, CountingService service) : ManagementPage
{
    [BindProperty] public CountInput Input { get; set; } = new();
    public List<Producto> Products { get; private set; } = [];
    public List<LoteExistencia> Lots { get; private set; } = [];
    public List<ConteoDetalle> SavedLines { get; private set; } = [];
    public Conteo? Saved { get; private set; }
    public bool Editable => Saved is null || Saved.Estado == "En_registro" && Saved.RegistradoPor == UserId;
    private async Task LoadAsync()
    {
        Products = await db.Set<Producto>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(); Lots = await db.Set<LoteExistencia>().AsNoTracking().OrderBy(x => x.ProductoId).ThenBy(x => x.Id).ToListAsync();
        if (Input.Id is long id) { Saved = await db.Set<Conteo>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); SavedLines = await db.Set<ConteoDetalle>().AsNoTracking().Where(x => x.ConteoId == id).OrderBy(x => x.Id).ToListAsync(); }
    }
    public async Task<IActionResult> OnGetAsync(long? id)
    {
        Input.Id = id; await LoadAsync();
        if (id is not null)
        {
            if (Saved is null) return NotFound();
            Input = new() { Id = Saved.Id, Version = Saved.Version, Reference = Saved.Referencia, Notes = Saved.Observaciones, Lines = SavedLines.Select(d => new CountLineInput { ProductId = d.ProductoId, LotId = d.LoteId, Physical = (d.CantidadFisicaEscalada / ReceivingService.Power(d.Escala)).ToString("0.######", CultureInfo.InvariantCulture), Note = d.Observacion }).ToList() };
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (ModelState.IsValid)
            {
                var lines = Input.Lines.Where(x => x.ProductId != 0 || x.LotId is not null || !string.IsNullOrWhiteSpace(x.Physical) || !string.IsNullOrWhiteSpace(x.Note)).Select(x => new CountingLine(x.ProductId, x.LotId, x.Physical, x.Note)).ToList();
                var id = await service.SaveAsync(UserId, new(Input.Id, Input.Version, Input.Reference, Input.Notes, Input.PhysicalChecked, lines)); TempData["Message"] = "Conteo guardado con saldo de referencia. Las existencias no se modificaron."; return RedirectToPage(new { id });
            }
        }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        await LoadAsync(); return Page();
    }
    public async Task<IActionResult> OnPostAddLineAsync()
    { ModelState.Clear(); if (Input.Lines.Count < 100) Input.Lines.Add(new()); else ModelState.AddModelError("", "Máximo 100 líneas."); await LoadAsync(); return Page(); }
    public async Task<IActionResult> OnPostFinalizeAsync()
    {
        var id = Input.Id; ModelState.Clear();
        try { await service.FinalizeAsync(UserId, id ?? 0, Input.Version); TempData["Message"] = "Conteo finalizado. La diferencia queda documentada; no se aplicó ningún ajuste de existencias."; return RedirectToPage(new { id }); }
        catch (BusinessException e) { await OnGetAsync(id); ModelState.Clear(); ModelState.AddModelError("", e.Message); return Page(); }
    }
}
