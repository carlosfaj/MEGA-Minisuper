using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.Json;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages.Recepciones;

[Authorize(Policy = "Inventario")]
public sealed class IndexModel(MegaDbContext db) : ManagementPage
{
    public List<Recepcion> Receipts { get; private set; } = [];
    public Dictionary<long, string> Suppliers { get; private set; } = [];
    public Dictionary<long, long> Pending { get; private set; } = [];
    public async Task OnGetAsync()
    {
        Receipts = await db.Set<Recepcion>().AsNoTracking().OrderByDescending(x => x.Id).Take(200).ToListAsync();
        Suppliers = await db.Set<Proveedor>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Nombre);
        Pending = await db.Set<Autorizacion>().AsNoTracking().Where(x => x.RecepcionId != null && x.Decision == "pendiente" && x.InvalidadaEn == null).ToDictionaryAsync(x => x.RecepcionId!.Value, x => x.Id);
    }
}
public sealed class ReceivingInput
{
    public long? Id { get; set; }
    public long Version { get; set; }
    [Required] public string Key { get; set; } = Guid.NewGuid().ToString();
    [Required(ErrorMessage = "Introduce una referencia única."), StringLength(100)] public string Reference { get; set; } = "";
    public long? SupplierId { get; set; }
    [StringLength(100)] public string? Document { get; set; }
    public string? DocumentDate { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
    public List<ReceivingLineInput> Lines { get; set; } = [new()];
}
public sealed class ReceivingLineInput
{
    public long PresentationId { get; set; }
    public string? Received { get; set; }
    public string? Accepted { get; set; }
    public string? BaseCost { get; set; }
    public string? Expiry { get; set; }
    [StringLength(100)] public string? Lot { get; set; }
    [StringLength(1000)] public string? Note { get; set; }
    public bool Empty => PresentationId == 0 && string.IsNullOrWhiteSpace(Received) && string.IsNullOrWhiteSpace(Accepted) && string.IsNullOrWhiteSpace(BaseCost) && string.IsNullOrWhiteSpace(Expiry) && string.IsNullOrWhiteSpace(Lot) && string.IsNullOrWhiteSpace(Note);
}
[Authorize(Policy = "Inventario")]
public sealed class EditarModel(MegaDbContext db, ReceivingService service) : ManagementPage
{
    [BindProperty] public ReceivingInput Input { get; set; } = new();
    public List<Proveedor> Suppliers { get; private set; } = [];
    public sealed record Option(long Id, string Text);
    public List<Option> Options { get; private set; } = [];
    public List<Autorizacion> Reviews { get; private set; } = [];
    public bool Editable { get; private set; } = true;
    public string State { get; private set; } = "Nueva";
    private async Task LoadAsync()
    {
        Suppliers = await db.Set<Proveedor>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync();
        var products = await db.Set<Producto>().AsNoTracking().ToDictionaryAsync(x => x.Id);
        Options = (await db.Set<Presentacion>().AsNoTracking().OrderBy(x => x.Id).ToListAsync()).Select(p => new Option(p.Id, $"{products[p.ProductoId].Nombre} · {p.Nombre} · {p.FactorEscalado / ReceivingService.Power(p.EscalaFactor):0.######} {products[p.ProductoId].UnidadBase} por presentación" + (p.Activo == 0 || products[p.ProductoId].Activo == 0 ? " (desactivada)" : ""))).ToList();
        if (Input.Id is long id)
        {
            var r = await db.Set<Recepcion>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
            Editable = r is not null && r.Estado != "Confirmada" && r.CreadoPor == UserId; State = r?.Estado ?? "No encontrada";
            Reviews = await db.Set<Autorizacion>().AsNoTracking().Where(x => x.RecepcionId == id).OrderByDescending(x => x.Id).ToListAsync();
        }
    }
    public async Task<IActionResult> OnGetAsync(long? id)
    {
        if (id is long key)
        {
            var r = await db.Set<Recepcion>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == key); if (r is null) return NotFound();
            Input = new() { Id = r.Id, Version = r.Version, Key = r.ClaveSolicitud, Reference = r.Referencia, SupplierId = r.ProveedorId, Document = r.Documento, DocumentDate = r.FechaDocumento, Notes = r.Observaciones, Lines = [] };
            foreach (var d in await db.Set<RecepcionDetalle>().AsNoTracking().Where(x => x.RecepcionId == key).OrderBy(x => x.NumeroLinea).ToListAsync())
                Input.Lines.Add(new() { PresentationId = d.PresentacionId, Received = Number(d.CantidadRecibidaEscalada, d.EscalaCantidad), Accepted = Number(d.CantidadAceptadaEscalada, d.EscalaCantidad), BaseCost = Number(d.CostoUnitEscalado, d.EscalaCosto ?? 6), Expiry = d.Vencimiento, Lot = d.LoteReferencia, Note = d.Observacion });
            if (Input.Lines.Count == 0) Input.Lines.Add(new());
        }
        await LoadAsync(); return Page();
    }
    private static string? Number(long? n, long scale) => n is null ? null : (n / ReceivingService.Power(scale)).Value.ToString("0.######", CultureInfo.InvariantCulture);
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (ModelState.IsValid)
            {
                var lines = Input.Lines.Where(x => !x.Empty).Select(x => new ReceivingLine(x.PresentationId, x.Received, x.Accepted, x.BaseCost, x.Expiry, x.Lot, x.Note)).ToList();
                var id = await service.SaveAsync(UserId, new(Input.Id, Input.Version, Input.Key, Input.Reference, Input.SupplierId, Input.Document, Input.DocumentDate, Input.Notes, lines));
                TempData["Message"] = "Preparación guardada. No se han modificado existencias."; return RedirectToPage(new { id });
            }
        }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        await LoadAsync(); return Page();
    }
    public async Task<IActionResult> OnPostAddLineAsync()
    {
        ModelState.Clear(); if (Input.Lines.Count < 100) Input.Lines.Add(new()); else ModelState.AddModelError("", "La recepción admite hasta 100 líneas.");
        await LoadAsync(); return Page();
    }
    public async Task<IActionResult> OnPostSubmitAsync()
    {
        ModelState.Clear();
        try { await service.SubmitAsync(UserId, Input.Id ?? 0, Input.Version); TempData["Message"] = "Recepción enviada. Otra cuenta de gestión debe revisar esta versión."; return RedirectToPage(new { id = Input.Id }); }
        catch (BusinessException e) { await OnGetAsync(Input.Id); ModelState.Clear(); ModelState.AddModelError("", e.Message); return Page(); }
    }
}
[Authorize(Policy = "Inventario")]
public sealed class RevisionModel(MegaDbContext db, ReceivingService service) : ManagementPage
{
    [BindProperty(SupportsGet = true)] public long Id { get; set; }
    [BindProperty] public long ExpectedVersion { get; set; }
    [BindProperty] public string ExpectedHash { get; set; } = "";
    [BindProperty] public string? Reason { get; set; }
    public Autorizacion Review { get; private set; } = null!;
    public ReceiptReview ReviewedContent { get; private set; } = null!;
    public bool Current { get; private set; }
    public bool CanDecide => Current && Review.Decision == "pendiente" && Review.SolicitanteId != UserId && (User.IsInRole("Administrador") || User.IsInRole("Propietario"));
    private async Task<bool> LoadAsync()
    {
        var auth = await db.Set<Autorizacion>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == Id && x.RecepcionId != null); if (auth is null) return false;
        if (Formats.Hash(auth.ContenidoSnapshot) != auth.ContenidoHash) throw new InvalidOperationException("El contenido de revisión requiere verificar integridad.");
        Review = auth; ReviewedContent = JsonSerializer.Deserialize<ReceiptReview>(auth.ContenidoSnapshot, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Current = auth.InvalidadaEn is null && Formats.Hash(Formats.Json(await service.ReviewAsync(auth.RecepcionId!.Value))) == auth.ContenidoHash;
        return true;
    }
    public async Task<IActionResult> OnGetAsync()
    {
        if (!await LoadAsync()) return NotFound(); ExpectedVersion = Review.ObjetoVersion; ExpectedHash = Review.ContenidoHash; return Page();
    }
    public Task<IActionResult> OnPostApproveAsync() => DecideAsync(true);
    public Task<IActionResult> OnPostRejectAsync() => DecideAsync(false);
    private async Task<IActionResult> DecideAsync(bool approve)
    {
        if (!User.IsInRole("Administrador") && !User.IsInRole("Propietario")) return Forbid();
        try
        {
            if (ModelState.IsValid) { await service.DecideAsync(UserId, Id, ExpectedVersion, ExpectedHash, approve, Reason ?? ""); TempData["Message"] = approve ? "Recepción confirmada. Se registró una única entrada de existencias." : "Recepción rechazada sin modificar existencias."; return RedirectToPage(new { id = Id }); }
        }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        if (!await LoadAsync()) return NotFound(); return Page();
    }
}
