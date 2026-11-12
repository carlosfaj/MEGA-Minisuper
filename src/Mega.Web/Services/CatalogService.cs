using System.Globalization;
using Mega.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Mega.Web.Services;

public sealed record ProductChange(long? Id, long Version, string Name, long CategoryId, string Control, string Unit,
    string Minimum, bool Expiration, bool Active);
public sealed record PresentationChange(long? Id, long ProductId, long ProductVersion, long Version, string Name, string Factor, bool Active);

public sealed class CatalogService(MegaDbContext db, AccessControl access, Operations operations)
{
    public static string CategoryHash(Categoria category) => Formats.Hash(Formats.Json(new { category.Id, category.Nombre, category.Activo }));
    public async Task<long> SaveCategoryAsync(long userId, long? id, string? expectedHash, string name, bool active)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.ManagerAsync(userId);
        name = AccessControl.Text(name, "Nombre de categoría");
        var rows = await db.Set<Categoria>().ToListAsync();
        if (rows.Any(x => x.Id != id && string.Equals(x.Nombre, name, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessException("Ya existe una categoría con ese nombre, incluso si está desactivada.");
        var category = id is null ? new Categoria() : rows.SingleOrDefault(x => x.Id == id) ?? throw new BusinessException("Categoría no encontrada.");
        if (id is not null && CategoryHash(category) != expectedHash) throw new BusinessException("La categoría cambió. Recarga antes de guardar.");
        var before = id is null ? null : new { category.Nombre, category.Activo };
        category.Nombre = name; category.Activo = active ? 1 : 0;
        if (id is null) db.Add(category);
        await db.SaveChangesAsync();
        operations.Audit(userId, id is null ? "Crear categoría" : "Actualizar categoría", "Categoria", category.Id.ToString(), Guid.NewGuid().ToString(), new { antes = before, despues = new { category.Nombre, category.Activo } });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return category.Id;
    }

    public async Task<long> SaveProductAsync(long userId, ProductChange input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.ManagerAsync(userId);
        var name = AccessControl.Text(input.Name, "Nombre de producto");
        var unit = AccessControl.Text(input.Unit, "Unidad base", 30);
        if (input.Control is not ("unidad" or "peso")) throw new BusinessException("Selecciona control por unidad o peso.");
        var category = await db.Set<Categoria>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.CategoryId);
        var product = input.Id is null ? new Producto { Version = 1 } : await db.Set<Producto>().SingleOrDefaultAsync(x => x.Id == input.Id)
            ?? throw new BusinessException("Producto no encontrado.");
        if (category is null || (category.Activo != 1 && (input.Id is null || category.Id != product.CategoriaId)))
            throw new BusinessException("Selecciona una categoría activa.");
        var scale = input.Control == "unidad" ? 0 : 3;
        var minimum = Formats.Scaled(Formats.Parse(input.Minimum), scale);
        if (input.Id is not null)
        {
            AccessControl.Version(product.Version, input.Version);
            var used = await db.Set<MovimientoInventario>().AnyAsync(x => x.ProductoId == product.Id) ||
                await db.Set<RecepcionDetalle>().AnyAsync(x => db.Set<Presentacion>().Any(p => p.Id == x.PresentacionId && p.ProductoId == product.Id));
            if (used && (product.UnidadControl != input.Control || product.UnidadBase != unit || product.EscalaCantidad != scale))
                throw new BusinessException("Este producto tiene historia. Conserva su unidad y escala; registra otro producto para una unidad diferente.");
        }
        var before = input.Id is null ? null : new { product.Nombre, product.CategoriaId, product.UnidadControl, product.UnidadBase, product.MinimoEscalado, product.Activo, product.ControlVencimiento };
        product.Nombre = name; product.CategoriaId = input.CategoryId; product.UnidadControl = input.Control; product.UnidadBase = unit;
        product.EscalaCantidad = scale; product.MinimoEscalado = minimum; product.ControlVencimiento = input.Expiration ? 1 : 0; product.Activo = input.Active ? 1 : 0;
        if (input.Id is null) db.Add(product); else product.Version++;
        await db.SaveChangesAsync();
        operations.Audit(userId, input.Id is null ? "Crear producto" : "Actualizar producto", "Producto", product.Id.ToString(), Guid.NewGuid().ToString(),
            new { antes = before, despues = new { product.Nombre, product.CategoriaId, product.UnidadControl, product.UnidadBase, product.MinimoEscalado, product.Activo, product.ControlVencimiento, product.Version } });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return product.Id;
    }

    public async Task<long> SavePresentationAsync(long userId, PresentationChange input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.ManagerAsync(userId);
        var product = await ProductAsync(input.ProductId, input.ProductVersion);
        var name = AccessControl.Text(input.Name, "Nombre de presentación");
        var factorValue = Formats.Parse(input.Factor);
        if (factorValue <= 0 || (product.UnidadControl == "unidad" && decimal.Truncate(factorValue) != factorValue))
            throw new BusinessException("La equivalencia debe ser positiva y entera para un producto indivisible.");
        var factor = Formats.Scaled(factorValue, 6);
        if (await db.Set<Presentacion>().AnyAsync(x => x.ProductoId == product.Id && x.Id != input.Id && x.Nombre == name))
            throw new BusinessException("Ese nombre de presentación ya existe en el producto.");
        var presentation = input.Id is null ? new Presentacion { ProductoId = product.Id, Version = 1 } :
            await db.Set<Presentacion>().SingleOrDefaultAsync(x => x.Id == input.Id && x.ProductoId == product.Id) ?? throw new BusinessException("Presentación no encontrada.");
        if (input.Id is not null)
        {
            AccessControl.Version(presentation.Version, input.Version);
            var used = await db.Set<VentaDetalle>().AnyAsync(x => x.PresentacionId == presentation.Id) || await db.Set<RecepcionDetalle>().AnyAsync(x => x.PresentacionId == presentation.Id);
            if (used && factorValue != presentation.FactorEscalado / (decimal)Math.Pow(10, presentation.EscalaFactor))
                throw new BusinessException("La presentación ya tiene historia. Crea otra presentación para una equivalencia diferente.");
        }
        var before = input.Id is null ? null : new { presentation.Nombre, presentation.FactorEscalado, presentation.EscalaFactor, presentation.Activo };
        presentation.Nombre = name; presentation.FactorEscalado = factor; presentation.EscalaFactor = 6; presentation.Activo = input.Active ? 1 : 0;
        product.Version++;
        if (input.Id is null) db.Add(presentation); else presentation.Version++;
        await db.SaveChangesAsync();
        operations.Audit(userId, input.Id is null ? "Crear presentación" : "Actualizar presentación", "Presentacion", presentation.Id.ToString(), Guid.NewGuid().ToString(),
            new { productoId = product.Id, antes = before, despues = new { presentation.Nombre, presentation.FactorEscalado, presentation.EscalaFactor, presentation.Activo, presentation.Version } });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return presentation.Id;
    }

    public async Task AddCodeAsync(long userId, long productId, long productVersion, long presentationId, string code)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.ManagerAsync(userId);
        var product = await ProductAsync(productId, productVersion);
        code = code?.Trim() ?? "";
        if (code.Length is 0 or >128 || code.Any(char.IsControl)) throw new BusinessException("Introduce un código de 1 a 128 caracteres, sin caracteres de control.");
        if (!await db.Set<Presentacion>().AnyAsync(x => x.Id == presentationId && x.ProductoId == productId && x.Activo == 1))
            throw new BusinessException("Selecciona una presentación activa de este producto.");
        if (await db.Set<CodigoProducto>().AnyAsync(x => x.Codigo == code)) throw new BusinessException("El código ya está asignado. Un código desactivado conserva su producto original.");
        var item = new CodigoProducto { PresentacionId = presentationId, Codigo = code, Activo = 1 }; db.Add(item); product.Version++;
        await db.SaveChangesAsync(); operations.Audit(userId, "Añadir código", "CodigoProducto", item.Id.ToString(), Guid.NewGuid().ToString(), new { productoId = productId, codigo = code, presentationId });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task SetCodeActiveAsync(long userId, long productId, long productVersion, long codeId, bool active)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.ManagerAsync(userId);
        var product = await ProductAsync(productId, productVersion);
        var code = await db.Set<CodigoProducto>().SingleOrDefaultAsync(x => x.Id == codeId && db.Set<Presentacion>().Any(p => p.Id == x.PresentacionId && p.ProductoId == productId))
            ?? throw new BusinessException("Código no encontrado en este producto.");
        code.Activo = active ? 1 : 0; product.Version++;
        operations.Audit(userId, active ? "Activar código" : "Desactivar código", "CodigoProducto", code.Id.ToString(), Guid.NewGuid().ToString(), new { productoId = productId, code.Codigo, code.Activo });
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task<long> SetPriceAsync(long userId, long productId, long productVersion, long presentationId, string amount, string? localStart)
    {
        await using var tx = await db.Database.BeginTransactionAsync(); await access.ManagerAsync(userId);
        var product = await ProductAsync(productId, productVersion);
        if (!await db.Set<Presentacion>().AnyAsync(x => x.Id == presentationId && x.ProductoId == productId && x.Activo == 1))
            throw new BusinessException("Selecciona una presentación activa de este producto.");
        var cents = Formats.Scaled(Formats.Parse(amount), 2);
        if (cents <= 0) throw new BusinessException("El precio debe ser mayor que cero; las ventas gratuitas siguen pendientes de definición.");
        var now = DateTimeOffset.UtcNow;
        var start = now;
        if (!string.IsNullOrWhiteSpace(localStart))
        {
            if (!DateTime.TryParseExact(localStart, ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
                throw new BusinessException("La fecha de inicio no es válida.");
            start = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), TimeSpan.FromHours(-6)).ToUniversalTime();
            if (start < now) throw new BusinessException("El precio nuevo debe comenzar ahora o en el futuro; no se reescribe el pasado.");
        }
        var prices = await db.Set<PrecioProducto>().Where(x => x.PresentacionId == presentationId).OrderBy(x => x.VigenteDesde).ToListAsync();
        var instant = start.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        if (prices.Any(x => x.VigenteDesde == instant)) throw new BusinessException("Ya existe una condición que comienza en ese instante. Recarga antes de continuar.");
        var containing = prices.SingleOrDefault(x => string.CompareOrdinal(x.VigenteDesde, instant) < 0 && (x.VigenteHasta is null || string.CompareOrdinal(x.VigenteHasta, instant) > 0));
        var next = prices.FirstOrDefault(x => string.CompareOrdinal(x.VigenteDesde, instant) > 0);
        string? end = next?.VigenteDesde;
        if (containing is not null)
        {
            end = containing.VigenteHasta is not null && (end is null || string.CompareOrdinal(containing.VigenteHasta, end) < 0) ? containing.VigenteHasta : end;
            containing.VigenteHasta = instant; await db.SaveChangesAsync();
        }
        var price = new PrecioProducto { PresentacionId = presentationId, PrecioCentavos = cents, VigenteDesde = instant, VigenteHasta = end, CreadoPor = userId, CreadoEn = Formats.Now() };
        db.Add(price); product.Version++; await db.SaveChangesAsync();
        operations.Audit(userId, "Registrar precio vigente", "PrecioProducto", price.Id.ToString(), Guid.NewGuid().ToString(),
            new { productoId = productId, presentationId, price.PrecioCentavos, price.VigenteDesde, price.VigenteHasta, condicionAnteriorId = containing?.Id });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return price.Id;
    }

    private async Task<Producto> ProductAsync(long id, long version)
    {
        var product = await db.Set<Producto>().SingleOrDefaultAsync(x => x.Id == id) ?? throw new BusinessException("Producto no encontrado.");
        AccessControl.Version(product.Version, version); return product;
    }
}
