using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Pages.Catalogo;

[Authorize(Policy = "Gestion")]
public sealed class IndexModel(MegaDbContext db) : ManagementPage
{
    public sealed record Row(Producto Product, string Category, int Presentations);
    public List<Row> Rows { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    public async Task OnGetAsync()
    {
        var categories = await db.Set<Categoria>().AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Nombre);
        var presentations = await db.Set<Presentacion>().AsNoTracking().ToListAsync();
        var codes = await db.Set<CodigoProducto>().AsNoTracking().ToListAsync();
        var products = await db.Set<Producto>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync();
        var search = Search?.Trim() ?? "";
        Rows = products.Where(p => search.Length == 0 || p.Nombre.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            codes.Any(c => c.Codigo.Contains(search, StringComparison.OrdinalIgnoreCase) && presentations.Any(pr => pr.Id == c.PresentacionId && pr.ProductoId == p.Id)))
            .Select(p => new Row(p, categories[p.CategoriaId], presentations.Count(pr => pr.ProductoId == p.Id))).ToList();
    }
}

public sealed class CategoryInput
{
    public long? Id { get; set; }
    public string? Hash { get; set; }
    [Required(ErrorMessage = "Introduce el nombre de categoría."), StringLength(200)] public string Name { get; set; } = "";
    public bool Active { get; set; } = true;
}
[Authorize(Policy = "Gestion")]
public sealed class CategoriasModel(MegaDbContext db, CatalogService service) : ManagementPage
{
    [BindProperty] public CategoryInput Input { get; set; } = new();
    public List<Categoria> Categories { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(long? id)
    {
        Categories = await db.Set<Categoria>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync();
        if (id is not null)
        {
            var item = Categories.SingleOrDefault(x => x.Id == id); if (item is null) return NotFound();
            Input = new() { Id = item.Id, Hash = CatalogService.CategoryHash(item), Name = item.Nombre, Active = item.Activo == 1 };
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try { if (ModelState.IsValid) { await service.SaveCategoryAsync(UserId, Input.Id, Input.Hash, Input.Name, Input.Active); TempData["Message"] = "Categoría guardada."; return RedirectToPage(); } }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        Categories = await db.Set<Categoria>().AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(); return Page();
    }
}

public sealed class ProductInput
{
    public long? Id { get; set; }
    public long Version { get; set; }
    [Required(ErrorMessage = "Introduce el nombre de producto."), StringLength(200)] public string Name { get; set; } = "";
    public long CategoryId { get; set; }
    [Required] public string Control { get; set; } = "unidad";
    [Required(ErrorMessage = "Introduce la unidad base."), StringLength(30)] public string Unit { get; set; } = "unidad";
    [Required] public string Minimum { get; set; } = "0";
    public bool Expiration { get; set; }
    public bool Active { get; set; } = true;
}
[Authorize(Policy = "Gestion")]
public sealed class ProductoModel(MegaDbContext db, CatalogService service) : ManagementPage
{
    [BindProperty] public ProductInput Input { get; set; } = new();
    public List<Categoria> Categories { get; private set; } = [];
    public List<Presentacion> Presentations { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(long? id)
    {
        if (id is not null)
        {
            var p = await db.Set<Producto>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); if (p is null) return NotFound();
            Input = new() { Id = p.Id, Version = p.Version, Name = p.Nombre, CategoryId = p.CategoriaId, Control = p.UnidadControl, Unit = p.UnidadBase,
                Minimum = (p.MinimoEscalado / (decimal)Math.Pow(10, p.EscalaCantidad)).ToString("0.###", CultureInfo.InvariantCulture), Expiration = p.ControlVencimiento == 1, Active = p.Activo == 1 };
        }
        await LoadAsync(); return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try { if (ModelState.IsValid) { var id = await service.SaveProductAsync(UserId, new(Input.Id, Input.Version, Input.Name, Input.CategoryId, Input.Control, Input.Unit, Input.Minimum, Input.Expiration, Input.Active)); TempData["Message"] = "Producto guardado. Puedes gestionar sus presentaciones, códigos y precios."; return RedirectToPage(new { id }); } }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        await LoadAsync(); return Page();
    }
    private async Task LoadAsync()
    {
        Categories = await db.Set<Categoria>().AsNoTracking().Where(x => x.Activo == 1 || x.Id == Input.CategoryId).OrderBy(x => x.Nombre).ToListAsync();
        if (Input.Id is long id) Presentations = await db.Set<Presentacion>().AsNoTracking().Where(x => x.ProductoId == id).OrderBy(x => x.Nombre).ToListAsync();
    }
}

public sealed class PresentationInput
{
    public long? Id { get; set; }
    public long ProductId { get; set; }
    public long ProductVersion { get; set; }
    public long Version { get; set; }
    [Required(ErrorMessage = "Introduce el nombre de presentación."), StringLength(200)] public string Name { get; set; } = "";
    [Required] public string Factor { get; set; } = "1";
    public bool Active { get; set; } = true;
}
[Authorize(Policy = "Gestion")]
public sealed class PresentacionModel(MegaDbContext db, CatalogService service) : ManagementPage
{
    [BindProperty] public PresentationInput Input { get; set; } = new();
    public Producto Product { get; private set; } = null!;
    public async Task<IActionResult> OnGetAsync(long productId, long? id)
    {
        Product = (await db.Set<Producto>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == productId))!; if (Product is null) return NotFound();
        Input = new() { ProductId = productId, ProductVersion = Product.Version };
        if (id is not null)
        {
            var p = await db.Set<Presentacion>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ProductoId == productId); if (p is null) return NotFound();
            Input.Id = p.Id; Input.Version = p.Version; Input.Name = p.Nombre; Input.Factor = (p.FactorEscalado / (decimal)Math.Pow(10, p.EscalaFactor)).ToString("0.######", CultureInfo.InvariantCulture); Input.Active = p.Activo == 1;
        }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try { if (ModelState.IsValid) { await service.SavePresentationAsync(UserId, new(Input.Id, Input.ProductId, Input.ProductVersion, Input.Version, Input.Name, Input.Factor, Input.Active)); TempData["Message"] = "Presentación guardada."; return RedirectToPage("/Catalogo/Producto", new { id = Input.ProductId }); } }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        Product = (await db.Set<Producto>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == Input.ProductId))!; return Product is null ? NotFound() : Page();
    }
}

public sealed class CodeInput
{
    public long ProductId { get; set; }
    public long ProductVersion { get; set; }
    public long PresentationId { get; set; }
    [Required(ErrorMessage = "Introduce el código."), StringLength(128)] public string Code { get; set; } = "";
}
[Authorize(Policy = "Gestion")]
public sealed class CodigosModel(MegaDbContext db, CatalogService service) : ManagementPage
{
    public CodeInput Input { get; set; } = new();
    public Producto Product { get; private set; } = null!;
    public List<Presentacion> Presentations { get; private set; } = [];
    public List<CodigoProducto> Codes { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(long productId)
    {
        if (!await LoadAsync(productId)) return NotFound(); Input.ProductId = productId; Input.ProductVersion = Product.Version; return Page();
    }
    public async Task<IActionResult> OnPostAsync([Bind(Prefix = "Input")] CodeInput input)
    {
        Input = input;
        try { if (ModelState.IsValid) { await service.AddCodeAsync(UserId, input.ProductId, input.ProductVersion, input.PresentationId, input.Code); TempData["Message"] = "Código registrado."; return RedirectToPage(new { productId = input.ProductId }); } }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        return await LoadAsync(input.ProductId) ? Page() : NotFound();
    }
    public async Task<IActionResult> OnPostEstadoAsync(long productId, long productVersion, long codeId, bool active)
    {
        try { if (ModelState.IsValid) { await service.SetCodeActiveAsync(UserId, productId, productVersion, codeId, active); TempData["Message"] = "Estado del código actualizado."; return RedirectToPage(new { productId }); } }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        if (!await LoadAsync(productId)) return NotFound(); Input.ProductId = productId; Input.ProductVersion = Product.Version; return Page();
    }
    private async Task<bool> LoadAsync(long id)
    {
        Product = (await db.Set<Producto>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id))!; if (Product is null) return false;
        Presentations = await db.Set<Presentacion>().AsNoTracking().Where(x => x.ProductoId == id).ToListAsync();
        var ids = Presentations.Select(x => x.Id).ToList(); Codes = await db.Set<CodigoProducto>().AsNoTracking().Where(x => ids.Contains(x.PresentacionId)).OrderBy(x => x.Codigo).ToListAsync(); return true;
    }
}

public sealed class PriceInput
{
    public long ProductId { get; set; }
    public long ProductVersion { get; set; }
    public long PresentationId { get; set; }
    [Required(ErrorMessage = "Introduce el precio."), StringLength(32)] public string Amount { get; set; } = "";
    public bool Schedule { get; set; }
    public string? LocalStart { get; set; }
}
[Authorize(Policy = "Gestion")]
public sealed class PrecioModel(MegaDbContext db, CatalogService service) : ManagementPage
{
    [BindProperty] public PriceInput Input { get; set; } = new();
    public Producto Product { get; private set; } = null!;
    public List<Presentacion> Presentations { get; private set; } = [];
    public List<PrecioProducto> Prices { get; private set; } = [];
    public async Task<IActionResult> OnGetAsync(long productId, long? presentationId)
    {
        if (!await LoadAsync(productId)) return NotFound();
        Input.ProductId = productId; Input.ProductVersion = Product.Version; Input.PresentationId = presentationId ?? Presentations.FirstOrDefault(x => x.Activo == 1)?.Id ?? 0; return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.Schedule && string.IsNullOrWhiteSpace(Input.LocalStart)) ModelState.AddModelError("", "Selecciona la fecha de inicio del precio programado.");
        try { if (ModelState.IsValid) { await service.SetPriceAsync(UserId, Input.ProductId, Input.ProductVersion, Input.PresentationId, Input.Amount, Input.Schedule ? Input.LocalStart : null); TempData["Message"] = "Precio registrado conservando las condiciones anteriores."; return RedirectToPage(new { productId = Input.ProductId, presentationId = Input.PresentationId }); } }
        catch (BusinessException e) { ModelState.AddModelError("", e.Message); }
        return await LoadAsync(Input.ProductId) ? Page() : NotFound();
    }
    private async Task<bool> LoadAsync(long id)
    {
        Product = (await db.Set<Producto>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id))!; if (Product is null) return false;
        Presentations = await db.Set<Presentacion>().AsNoTracking().Where(x => x.ProductoId == id).ToListAsync();
        var ids = Presentations.Select(x => x.Id).ToList(); Prices = await db.Set<PrecioProducto>().AsNoTracking().Where(x => ids.Contains(x.PresentacionId)).OrderByDescending(x => x.VigenteDesde).ToListAsync(); return true;
    }
}
