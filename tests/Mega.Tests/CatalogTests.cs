using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;

public static class HttpTestSupport
{
    public static async Task<string> TokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path); response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync();
        var token = Regex.Match(text, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token); return token;
    }
    public static async Task LoginAsync(HttpClient client, string name = "admin", string password = "MegaDemo2026!")
    {
        var token = await TokenAsync(client, "/Acceso");
        var result = await client.PostAsync("/Acceso", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Account"] = name, ["Password"] = password, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
    }
    public static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, Dictionary<string, string> values, string? tokenPage = null)
    {
        values["__RequestVerificationToken"] = await TokenAsync(client, tokenPage ?? path.Split('?')[0]);
        return await client.PostAsync(path, new FormUrlEncodedContent(values));
    }
}

public sealed class CatalogTests
{
    [Fact]
    public async Task catalog_can_create_category_product_presentation_code_price_and_sell_it()
    {
        using var factory = new MegaFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MegaDbContext>(); var service = scope.ServiceProvider.GetRequiredService<CatalogService>();
        var op = scope.ServiceProvider.GetRequiredService<Operations>();
        var category = await service.SaveCategoryAsync(1, null, null, "Bebidas", true);
        var id = await service.SaveProductAsync(1, new(null, 0, "Agua de prueba", category, "unidad", "botella", "3", false, true));
        var product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == id);
        var presentation = await service.SavePresentationAsync(1, new(null, id, product.Version, 0, "Botella", "1", true));
        product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == id);
        await service.AddCodeAsync(1, id, product.Version, presentation, "000012345");
        product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == id);
        await service.SetPriceAsync(1, id, product.Version, presentation, "20.50", null);
        Assert.Equal("000012345", (await db.Set<CodigoProducto>().SingleAsync(x => x.PresentacionId == presentation)).Codigo);
        await op.OpenCashAsync(2, "500", null);
        List<LineInput> lines = [new(presentation, "unidad", "2")]; var quote = await op.QuoteAsync(2, lines);
        var sale = await op.ConfirmAsync(2, new(Guid.NewGuid().ToString(), quote.Hash, "50", true, lines));
        Assert.Equal(4100, (await db.Set<Venta>().SingleAsync(x => x.Id == sale)).TotalCentavos);
        Assert.Equal(-2m, (await op.StocksAsync())[id]);
        var connection = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection(); await connection.OpenAsync();
        Assert.True((await scope.ServiceProvider.GetRequiredService<DatabaseInspector>().InspectAsync(connection, db.Model)).Healthy);
    }

    [Fact]
    public async Task T01_price_and_name_changes_preserve_receipt_and_disable_only_future_sales()
    {
        using var factory = new MegaFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MegaDbContext>(); var service = scope.ServiceProvider.GetRequiredService<CatalogService>(); var op = scope.ServiceProvider.GetRequiredService<Operations>();
        await op.OpenCashAsync(2, "500", null); List<LineInput> lines = [new(2, "unidad", "1")];
        var quote = await op.QuoteAsync(2, lines); var saleId = await op.ConfirmAsync(2, new(Guid.NewGuid().ToString(), quote.Hash, "50", false, lines));
        var original = (await db.Set<Venta>().AsNoTracking().SingleAsync(x => x.Id == saleId)).ComprobanteSnapshot;
        var product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == 2);
        await service.SetPriceAsync(1, 2, product.Version, 2, "40", null);
        Assert.Equal(4000, (await op.QuoteAsync(2, lines)).Total);
        product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == 2);
        await service.SaveProductAsync(1, new(2, product.Version, "Leche renombrada", product.CategoriaId, product.UnidadControl, product.UnidadBase, "5", false, false));
        await Assert.ThrowsAsync<BusinessException>(() => op.QuoteAsync(2, lines));
        Assert.Equal(original, (await db.Set<Venta>().AsNoTracking().SingleAsync(x => x.Id == saleId)).ComprobanteSnapshot);
        Assert.Equal(2, await db.Set<PrecioProducto>().CountAsync(x => x.PresentacionId == 2));
    }

    [Fact]
    public async Task used_units_and_factors_cannot_be_rewritten()
    {
        using var factory = new MegaFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MegaDbContext>(); var service = scope.ServiceProvider.GetRequiredService<CatalogService>(); var op = scope.ServiceProvider.GetRequiredService<Operations>();
        var product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == 1);
        await Assert.ThrowsAsync<BusinessException>(() => service.SaveProductAsync(1, new(1, product.Version, product.Nombre, product.CategoriaId, "peso", "kg", "0", false, true)));
        await op.OpenCashAsync(2, "500", null); List<LineInput> lines = [new(1, "peso_manual", "1")]; var quote = await op.QuoteAsync(2, lines);
        await op.ConfirmAsync(2, new(Guid.NewGuid().ToString(), quote.Hash, "30", false, lines));
        await Assert.ThrowsAsync<BusinessException>(() => service.SavePresentationAsync(1, new(1, 1, product.Version, 1, "Libra", "2", true)));
    }

    [Fact]
    public async Task stale_product_version_and_reused_disabled_codes_are_rejected()
    {
        using var factory = new MegaFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MegaDbContext>(); var service = scope.ServiceProvider.GetRequiredService<CatalogService>();
        var product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == 1);
        await service.AddCodeAsync(1, 1, product.Version, 1, "000001");
        await Assert.ThrowsAsync<BusinessException>(() => service.AddCodeAsync(1, 1, product.Version, 1, "000002"));
        product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == 1); var code = await db.Set<CodigoProducto>().SingleAsync(x => x.Codigo == "000001");
        await service.SetCodeActiveAsync(1, 1, product.Version, code.Id, false);
        product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == 1);
        await Assert.ThrowsAsync<BusinessException>(() => service.AddCodeAsync(1, 1, product.Version, 1, "000001"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task cashier_and_inventory_cannot_change_catalog(long actor)
    {
        using var factory = new MegaFactory(); using var scope = factory.Services.CreateScope();
        await Assert.ThrowsAsync<BusinessException>(() => scope.ServiceProvider.GetRequiredService<CatalogService>().SaveCategoryAsync(actor, null, null, "Sin permiso", true));
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<MegaDbContext>().Set<Categoria>().CountAsync());
    }

    [Fact]
    public async Task scheduled_prices_preserve_neighbors_and_cannot_be_backdated()
    {
        using var factory = new MegaFactory(); using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MegaDbContext>(); var service = scope.ServiceProvider.GetRequiredService<CatalogService>();
        var product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == 1);
        await service.SetPriceAsync(1, 1, product.Version, 1, "30", "2099-02-01T08:00");
        product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == 1);
        await service.SetPriceAsync(1, 1, product.Version, 1, "28", "2099-01-01T08:00");
        var prices = await db.Set<PrecioProducto>().Where(x => x.PresentacionId == 1).OrderBy(x => x.VigenteDesde).ToListAsync();
        Assert.Equal(3, prices.Count); Assert.Equal(prices[1].VigenteDesde, prices[0].VigenteHasta); Assert.Equal(prices[2].VigenteDesde, prices[1].VigenteHasta);
        product = await db.Set<Producto>().AsNoTracking().SingleAsync(x => x.Id == 1);
        await Assert.ThrowsAsync<BusinessException>(() => service.SetPriceAsync(1, 1, product.Version, 1, "10", "2020-01-01T00:00"));
    }

    [Fact]
    public async Task category_names_are_normalized_and_stale_edits_are_rejected()
    {
        using var factory = new MegaFactory(); using var scope = factory.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<CatalogService>();
        var db = scope.ServiceProvider.GetRequiredService<MegaDbContext>(); var id = await service.SaveCategoryAsync(1, null, null, "  Bebidas  ", true);
        await Assert.ThrowsAsync<BusinessException>(() => service.SaveCategoryAsync(1, null, null, "bebidas", true));
        var item = await db.Set<Categoria>().AsNoTracking().SingleAsync(x => x.Id == id); var hash = CatalogService.CategoryHash(item);
        await service.SaveCategoryAsync(1, id, hash, "Bebidas frías", true);
        await Assert.ThrowsAsync<BusinessException>(() => service.SaveCategoryAsync(1, id, hash, "Otro nombre", true));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.123")]
    [InlineData("9999999999999999999999999999")]
    public async Task prices_reject_invalid_money_without_changing_existing_condition(string amount)
    {
        using var factory = new MegaFactory(); using var scope = factory.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<CatalogService>();
        await Assert.ThrowsAsync<BusinessException>(() => service.SetPriceAsync(1, 1, 1, 1, amount, null));
        var price = await scope.ServiceProvider.GetRequiredService<MegaDbContext>().Set<PrecioProducto>().SingleAsync(x => x.PresentacionId == 1);
        Assert.Null(price.VigenteHasta); Assert.Equal(2400, price.PrecioCentavos);
    }

    [Fact]
    public async Task management_forms_create_real_records_and_enforce_http_permissions()
    {
        using var factory = new MegaFactory(); using var client = factory.Client(); await HttpTestSupport.LoginAsync(client);
        var response = await HttpTestSupport.PostAsync(client, "/Catalogo/Categorias", new() { ["Input.Name"] = "Categoría desde formulario", ["Input.Active"] = "true" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        long category;
        using (var scope = factory.Services.CreateScope()) category = (await scope.ServiceProvider.GetRequiredService<MegaDbContext>().Set<Categoria>().SingleAsync(x => x.Nombre == "Categoría desde formulario")).Id;
        response = await HttpTestSupport.PostAsync(client, "/Catalogo/Producto", new() { ["Input.Name"] = "Producto de formulario", ["Input.CategoryId"] = category.ToString(CultureInfo.InvariantCulture), ["Input.Control"] = "unidad", ["Input.Unit"] = "unidad", ["Input.Minimum"] = "0", ["Input.Active"] = "true" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        foreach (var path in new[] { "/Catalogo", "/Catalogo/Producto/1", "/Catalogo/Presentacion?productId=1", "/Catalogo/Codigos?productId=1", "/Catalogo/Precio?productId=1" })
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        using var inventory = factory.Client(); await HttpTestSupport.LoginAsync(inventory, "inventario");
        Assert.Equal(HttpStatusCode.Redirect, (await inventory.GetAsync("/Catalogo")).StatusCode);
    }

    [Fact]
    public async Task opening_login_page_does_not_consume_password_attempt_limit()
    {
        using var factory = new MegaFactory(); using var client = factory.Client();
        for (var i = 0; i < 15; i++) Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Acceso")).StatusCode);
        await HttpTestSupport.LoginAsync(client);
    }

    [Fact]
    public async Task repeated_wrong_passwords_are_still_limited()
    {
        using var factory = new MegaFactory(); using var client = factory.Client();
        for (var i = 0; i < 10; i++)
        {
            var token = await HttpTestSupport.TokenAsync(client, "/Acceso");
            var response = await client.PostAsync("/Acceso", new FormUrlEncodedContent(new Dictionary<string, string>
                { ["Account"] = "admin", ["Password"] = "Incorrecta", ["__RequestVerificationToken"] = token }));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        var rejected = await client.PostAsync("/Acceso", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode); Assert.Equal("60", rejected.Headers.GetValues("Retry-After").Single());
    }
}
