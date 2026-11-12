using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;

public sealed class MegaFactory:WebApplicationFactory<Program>
{
    private readonly string dbPath=Path.Combine(Path.GetTempPath(),"mega-tests-"+Guid.NewGuid()+".db");
    private readonly bool demoData;
    private readonly string? bootstrapPassword;
    public MegaFactory(bool demoData=true,string? bootstrapPassword=null) { this.demoData=demoData; this.bootstrapPassword=bootstrapPassword; }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_,c)=>c.AddInMemoryCollection(new Dictionary<string,string?> { ["MEGA:DatabasePath"]=dbPath,["MEGA:DemoData"]=demoData.ToString(),["MEGA_BOOTSTRAP_PASSWORD"]=bootstrapPassword }));
    }
    public HttpClient Client()=>CreateClient(new WebApplicationFactoryClientOptions {BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing&&File.Exists(dbPath)) File.Delete(dbPath);}
}

public sealed class FlowTests
{
    private static List<LineInput> Basket()=>[new(1,"peso_manual","2.000"),new(2,"unidad","1")];
    private static SaleInput Input(Quote quote,string received="100")=>new(Guid.NewGuid().ToString(),quote.Hash,received,false,Basket());
    private static async Task<string> Token(HttpClient client,string page)
    {
        var response=await client.GetAsync(page);var html=await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        return Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
    }
    private static async Task Login(HttpClient client,string account="cajero")
    {
        var token=await Token(client,"/Acceso");Assert.NotEmpty(token);
        var response=await client.PostAsync("/Acceso",new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"]=token,["Account"]=account,["Password"]="MegaDemo2026!" }));
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
    }
    [Fact]
    public async Task T05_sale_is_atomic_and_repeat_recovers_one_sale()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();var db=scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        await op.OpenCashAsync(2,"480","Recibido C$20 menos del habitual");
        var quote=await op.QuoteAsync(2,Basket());Assert.Equal(8600,quote.Total);
        var input=Input(quote);var id=await op.ConfirmAsync(2,input);db.ChangeTracker.Clear();
        Assert.Equal(id,await op.ConfirmAsync(2,input));Assert.Equal(1,await db.Set<Venta>().CountAsync());
        var payment=await db.Set<Pago>().SingleAsync();Assert.Equal(8600,payment.ImporteCentavos);Assert.Equal(1400,payment.CambioCentavos);
        Assert.Equal(1,await db.Set<MovimientoCaja>().CountAsync());Assert.Equal(8600,(await db.Set<MovimientoCaja>().SingleAsync()).ImporteCentavos);
        Assert.Equal(2,await db.Set<MovimientoInventario>().CountAsync(x=>x.VentaDetalleId!=null));
        Assert.Equal(1,await db.Set<SolicitudImpresion>().CountAsync());Assert.Equal("Pendiente",(await db.Set<SolicitudImpresion>().SingleAsync()).Estado);
        Assert.Equal(48m,(await op.StocksAsync())[1]);Assert.Equal(29m,(await op.StocksAsync())[2]);
        Assert.Equal(56600,(await op.CashAsync(2)).Expected);
        var sale=await db.Set<Venta>().SingleAsync();Assert.Equal(Formats.Hash(sale.ComprobanteSnapshot),sale.ComprobanteHash);
    }
    [Fact]
    public async Task T05_insufficient_payment_leaves_no_partial_effects()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();var db=scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        await op.OpenCashAsync(2,"500",null);var input=Input(await op.QuoteAsync(2,Basket()),"80");
        await Assert.ThrowsAsync<BusinessException>(()=>op.ConfirmAsync(2,input));db.ChangeTracker.Clear();
        Assert.Empty(await db.Set<Venta>().ToListAsync());Assert.Empty(await db.Set<Pago>().ToListAsync());Assert.Empty(await db.Set<MovimientoCaja>().ToListAsync());
        Assert.Equal(1,await db.Set<OperacionInventario>().CountAsync());Assert.Equal(50m,(await op.StocksAsync())[1]);
    }
    [Fact]
    public async Task rollback_after_late_database_failure_removes_sale_payment_and_stock_effects()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();var db=scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        await op.OpenCashAsync(2,"500",null);var input=Input(await op.QuoteAsync(2,Basket()));
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_print BEFORE INSERT ON SolicitudImpresion BEGIN SELECT RAISE(ABORT, 'Prueba fallo tardio'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>op.ConfirmAsync(2,input));db.ChangeTracker.Clear();
        Assert.Equal(0,await db.Set<Venta>().CountAsync());Assert.Equal(0,await db.Set<Pago>().CountAsync());Assert.Equal(0,await db.Set<VentaDetalle>().CountAsync());
        Assert.Equal(0,await db.Set<MovimientoCaja>().CountAsync());Assert.Equal(1,await db.Set<OperacionInventario>().CountAsync());Assert.Equal(50m,(await op.StocksAsync())[1]);
    }
    [Fact]
    public async Task T08_one_station_rejects_two_open_shifts_and_requires_difference_reason()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();
        await Assert.ThrowsAsync<BusinessException>(()=>op.OpenCashAsync(2,"480",null));
        await op.OpenCashAsync(2,"480","Fondo menor");
        await Assert.ThrowsAsync<BusinessException>(()=>op.OpenCashAsync(2,"500",null));
        await Assert.ThrowsAsync<BusinessException>(()=>op.OpenCashAsync(4,"500",null));
    }
    [Fact]
    public async Task T09_close_saves_expected_count_difference_and_keeps_snapshot()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();var db=scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        await op.OpenCashAsync(2,"480","Fondo menor");await op.ConfirmAsync(2,Input(await op.QuoteAsync(2,Basket())));
        var shift=(await op.CashAsync(2)).Shift!;
        await Assert.ThrowsAsync<BusinessException>(()=>op.CloseCashAsync(2,shift.Id,shift.Version,"560",null));db.ChangeTracker.Clear();
        await op.CloseCashAsync(2,shift.Id,shift.Version,"560","Faltan C$6 al contar");db.ChangeTracker.Clear();
        var closed=await db.Set<TurnoCaja>().SingleAsync();Assert.Equal("Cerrado",closed.Estado);Assert.Equal(56600,closed.EfectivoEsperadoCierreCentavos);Assert.Equal(56000,closed.EfectivoContadoCentavos);Assert.Equal(-600,closed.DiferenciaCentavos);
        await Assert.ThrowsAsync<BusinessException>(()=>op.QuoteAsync(2,Basket()));
    }
    [Fact]
    public async Task stale_close_cannot_hide_a_new_sale()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();
        var id=await op.OpenCashAsync(2,"500",null);var old=(await op.CashAsync(2)).Shift!;
        await op.ConfirmAsync(2,Input(await op.QuoteAsync(2,Basket())));
        await Assert.ThrowsAsync<BusinessException>(()=>op.CloseCashAsync(2,id,old.Version,"586",null));
    }
    [Fact]
    public async Task price_change_requires_review_before_payment()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();var db=scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        await op.OpenCashAsync(2,"500",null);var input=Input(await op.QuoteAsync(2,Basket()));
        var price=await db.Set<PrecioProducto>().SingleAsync(x=>x.PresentacionId==1);price.PrecioCentavos=2500;await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessException>(()=>op.ConfirmAsync(2,input));Assert.Equal(0,await db.Set<Venta>().CountAsync());
    }
    [Fact]
    public async Task same_key_with_other_content_or_account_is_rejected()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();
        await op.OpenCashAsync(2,"500",null);var input=Input(await op.QuoteAsync(2,Basket()));await op.ConfirmAsync(2,input);
        await Assert.ThrowsAsync<BusinessException>(()=>op.ConfirmAsync(2,input with {Received="110"}));
        await Assert.ThrowsAsync<BusinessException>(()=>op.ConfirmAsync(4,input));
    }
    [Theory]
    [InlineData("unidad","1.5",2)]
    [InlineData("peso_manual","-1",1)]
    [InlineData("peso_manual","0",1)]
    [InlineData("peso_manual","1.0001",1)]
    [InlineData("peso_manual","9999999999999999999999999999",1)]
    [InlineData("importe","2",2)]
    [InlineData("desconocida","1",1)]
    public async Task quantity_mode_and_overflow_are_validated(string mode,string value,long presentation)
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();
        await op.OpenCashAsync(2,"500",null);
        await Assert.ThrowsAnyAsync<Exception>(()=>op.QuoteAsync(2,[new(presentation,mode,value)]));
    }
    [Fact]
    public async Task sales_by_amount_show_actual_rounded_quantity_and_total()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();
        await op.OpenCashAsync(2,"500",null);var quote=await op.QuoteAsync(2,[new(1,"importe","10")]);
        Assert.Equal(417,quote.Lines[0].Quantity);Assert.Equal(1001,quote.Total);
    }
    [Fact]
    public async Task negative_stock_requires_physical_check_without_blocking_by_sign()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();
        await op.OpenCashAsync(2,"500",null);var lines=new List<LineInput>{new(2,"unidad","31")};var quote=await op.QuoteAsync(2,lines);Assert.True(quote.NeedsPhysicalCheck);
        var input=new SaleInput(Guid.NewGuid().ToString(),quote.Hash,"1200",false,lines);
        await Assert.ThrowsAsync<BusinessException>(()=>op.ConfirmAsync(2,input));
        await op.ConfirmAsync(2,input with {PhysicalStockChecked=true});Assert.Equal(-1m,(await op.StocksAsync())[2]);
    }
    [Fact]
    public async Task database_foreign_keys_and_39_tables_match_reference()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        await db.Database.OpenConnectionAsync();await using var cmd=db.Database.GetDbConnection().CreateCommand();cmd.CommandText="PRAGMA foreign_keys";Assert.Equal(1L,await cmd.ExecuteScalarAsync());
        cmd.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";Assert.Equal(39L,await cmd.ExecuteScalarAsync());
        cmd.CommandText="PRAGMA foreign_key_check";await using var reader=await cmd.ExecuteReaderAsync();Assert.False(await reader.ReadAsync());
    }
    [Fact]
    public async Task real_http_flow_checks_login_csrf_sale_and_role_boundaries()
    {
        using var factory=new MegaFactory();using var cashier=factory.Client();
        Assert.Equal(HttpStatusCode.Redirect,(await cashier.GetAsync("/Venta")).StatusCode);
        await Login(cashier);var cashToken=await Token(cashier,"/Caja");
        Assert.Equal(HttpStatusCode.BadRequest,(await cashier.PostAsync("/Caja?handler=Abrir",new FormUrlEncodedContent(new Dictionary<string,string>{["Fund"]="500"}))).StatusCode);
        var opened=await cashier.PostAsync("/Caja?handler=Abrir",new FormUrlEncodedContent(new Dictionary<string,string>{["Fund"]="500",["__RequestVerificationToken"]=cashToken}));Assert.Equal(HttpStatusCode.Redirect,opened.StatusCode);
        var saleToken=await Token(cashier,"/Venta");cashier.DefaultRequestHeaders.Add("X-CSRF-TOKEN",saleToken);
        var quoted=await cashier.PostAsJsonAsync("/Venta?handler=Cotizar",Basket());quoted.EnsureSuccessStatusCode();var quote=await quoted.Content.ReadFromJsonAsync<Quote>();
        var confirmed=await cashier.PostAsJsonAsync("/Venta?handler=Confirmar",Input(quote!));confirmed.EnsureSuccessStatusCode();
        var body=await confirmed.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();var receipt=await cashier.GetAsync(body.GetProperty("url").GetString());Assert.Equal(HttpStatusCode.OK,receipt.StatusCode);
        Assert.Contains("C$ 86.00",await receipt.Content.ReadAsStringAsync());
        using var inventory=factory.Client();await Login(inventory,"inventario");
        Assert.Equal(HttpStatusCode.OK,(await inventory.GetAsync("/Existencias")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await inventory.GetAsync("/Venta")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await inventory.GetAsync(body.GetProperty("url").GetString())).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect,(await cashier.GetAsync("/Bitacora")).StatusCode);
    }
    [Fact]
    public async Task credential_version_change_invalidates_existing_session()
    {
        using var factory=new MegaFactory();using var client=factory.Client();await Login(client);
        using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<MegaDbContext>();var user=await db.Set<Usuario>().SingleAsync(x=>x.Id==2);user.CredencialesVersion++;await db.SaveChangesAsync();}
        Assert.Equal(HttpStatusCode.Redirect,(await client.GetAsync("/Index")).StatusCode);
    }
    [Fact]
    public async Task concurrent_retries_create_a_single_business_operation()
    {
        using var factory=new MegaFactory();SaleInput input;
        using(var scope=factory.Services.CreateScope()){var op=scope.ServiceProvider.GetRequiredService<Operations>();await op.OpenCashAsync(2,"500",null);input=Input(await op.QuoteAsync(2,Basket()));}
        async Task<long> Confirm(){using var scope=factory.Services.CreateScope();return await scope.ServiceProvider.GetRequiredService<Operations>().ConfirmAsync(2,input);}
        var results=await Task.WhenAll(Task.Run(Confirm),Task.Run(Confirm));Assert.Equal(results[0],results[1]);
        using var check=factory.Services.CreateScope();Assert.Equal(1,await check.ServiceProvider.GetRequiredService<MegaDbContext>().Set<Venta>().CountAsync());
    }
    [Fact]
    public async Task known_cost_product_preserves_valuation_when_sold()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();var op=scope.ServiceProvider.GetRequiredService<Operations>();var db=scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        await op.OpenCashAsync(2,"500",null);
        var movement=await db.Set<MovimientoInventario>().FirstAsync(x=>x.ProductoId==1);
        movement.EstadoCosto="conocido";movement.CostoUnitEscalado=18000000;movement.EscalaCosto=6;await db.SaveChangesAsync();
        var quote=await op.QuoteAsync(2,Basket());await op.ConfirmAsync(2,Input(quote));
        var detail=await db.Set<VentaDetalle>().SingleAsync(x=>x.ProductoId==1);Assert.Equal("conocido",detail.EstadoCosto);Assert.Equal(18000000,detail.CostoUnitEscalado);
    }
    [Fact]
    public async Task existing_demo_database_cannot_be_switched_to_real_mode()
    {
        using var factory=new MegaFactory();using var scope=factory.Services.CreateScope();
        var config=scope.ServiceProvider.GetRequiredService<IConfiguration>();config["MEGA:DemoData"]="false";
        await Assert.ThrowsAsync<InvalidOperationException>(()=>scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync());
    }
}
