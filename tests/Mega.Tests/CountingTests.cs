using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;

public sealed class CountingTests
{
    private sealed class Fixture : IDisposable
    {
        public MegaFactory Factory { get; } = new(); public IServiceScope Scope { get; }
        public MegaDbContext Db => Scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        public CountingService Service => Scope.ServiceProvider.GetRequiredService<CountingService>();
        public Operations Op => Scope.ServiceProvider.GetRequiredService<Operations>();
        public Fixture() => Scope = Factory.Services.CreateScope();
        public CountingChange Input(List<CountingLine>? lines = null) => new(null, 0, "CON-" + Guid.NewGuid().ToString("N"), "Prueba ficticia", true, lines ?? [new(2, null, "28", "Conteo físico ficticio")]);
        public async Task Sale(long presentation)
        { await Op.OpenCashAsync(2, "500", null); List<LineInput> lines = [new(presentation, presentation == 1 ? "peso_manual" : "unidad", "1")]; var quote = await Op.QuoteAsync(2, lines); await Op.ConfirmAsync(2, new(Guid.NewGuid().ToString(), quote.Hash, "100", false, lines)); }
        public void Dispose() { Scope.Dispose(); Factory.Dispose(); }
    }
    [Fact]
    public async Task T04_save_and_finalize_preserve_difference_without_changing_stock_or_cost()
    {
        using var f = new Fixture(); var input = f.Input(); var id = await f.Service.SaveAsync(3, input);
        var detail = await f.Db.Set<ConteoDetalle>().SingleAsync(); Assert.Equal(30, detail.SaldoReferenciaEscalado); Assert.Equal(28, detail.CantidadFisicaEscalada);
        await f.Service.FinalizeAsync(3, id, 1); await f.Service.FinalizeAsync(3, id, 1); f.Db.ChangeTracker.Clear();
        Assert.Equal(30m, (await f.Op.StocksAsync())[2]); Assert.Equal(1, await f.Db.Set<OperacionInventario>().CountAsync()); Assert.Empty(await f.Db.Set<SolicitudInventario>().Where(x => x.Referencia != "DEMO-CARGA-001").ToListAsync());
        Assert.Equal("Finalizado", (await f.Db.Set<Conteo>().SingleAsync()).Estado);
        var audit = await f.Db.Set<EventoAuditoria>().SingleAsync(x => x.Accion == "Finalizar conteo físico"); Assert.Contains("\"estado\":\"Finalizado\"", audit.DatosCambio);
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(3, input with { Id = id, Version = 2 }));
    }
    [Fact]
    public async Task movement_after_count_requires_new_physical_check_and_version()
    {
        using var f = new Fixture(); var input = f.Input(); var id = await f.Service.SaveAsync(3, input); await f.Sale(2);
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.FinalizeAsync(3, id, 1)); f.Db.ChangeTracker.Clear();
        await f.Service.SaveAsync(3, input with { Id = id, Version = 1, Lines = [new(2,null,"28","Volví a contar")] });
        await f.Service.FinalizeAsync(3, id, 2); Assert.Equal(29m, (await f.Op.StocksAsync())[2]);
        var d = await f.Db.Set<ConteoDetalle>().SingleAsync(); Assert.Equal(29, d.SaldoReferenciaEscalado);
    }
    [Fact]
    public async Task other_products_moving_do_not_invalidate_a_count()
    {
        using var f = new Fixture(); var id = await f.Service.SaveAsync(3, f.Input([new(1,null,"49.500",null)])); await f.Sale(2);
        await f.Service.FinalizeAsync(3,id,1); Assert.Equal(50m,(await f.Op.StocksAsync())[1]);
    }
    [Fact]
    public async Task invalid_counts_duplicates_mixed_scope_and_missing_physical_declaration_fail()
    {
        using var f = new Fixture(); var input = f.Input();
        foreach (var change in new[] { input with { PhysicalChecked = false }, input with { Lines = [] }, input with { Lines = [new(2,null,"1.5",null)] }, input with { Lines = [new(2,null,"1",null),new(2,null,"2",null)] }, input with { Lines = [new(2,null,"-1",null)] } })
        { await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(3,change)); f.Db.ChangeTracker.Clear(); }
        Assert.Empty(await f.Db.Set<Conteo>().ToListAsync()); Assert.Equal(30m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task permissions_owner_and_stale_edits_are_checked_in_service_and_http()
    {
        using var f = new Fixture(); var input = f.Input(); await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(2,input));
        var id = await f.Service.SaveAsync(3,input); await Assert.ThrowsAsync<BusinessException>(() => f.Service.FinalizeAsync(1,id,1));
        await f.Service.SaveAsync(3,input with { Id = id, Version = 1 });
        await Assert.ThrowsAsync<BusinessException>(() => f.Service.SaveAsync(3,input with { Id = id, Version = 1 }));
        using var client = f.Factory.Client(); await HttpTestSupport.LoginAsync(client,"cajero");
        Assert.Contains("SinPermiso",(await client.GetAsync("/Conteos")).Headers.Location!.ToString()); Assert.Contains("SinPermiso",(await client.GetAsync("/Lotes")).Headers.Location!.ToString());
    }
    [Fact]
    public async Task lot_count_checks_product_scope_and_keeps_total_distinct()
    {
        using var f = new Fixture(); var receive = f.Scope.ServiceProvider.GetRequiredService<ReceivingService>(); var supplier = await receive.SaveSupplierAsync(1,new(null,0,"Proveedor lote",null,null,null,null,true));
        var receipt = await receive.SaveAsync(3,new(null,0,Guid.NewGuid().ToString(),"REC-LOTE",supplier,"DOC","2026-11-12",null,[new(2,"8","8","20","2030-01-01","L-001",null)]));
        var auth = await receive.SubmitAsync(3,receipt,1); var hash = (await f.Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==auth)).ContenidoHash;
        await receive.DecideAsync(1,auth,1,hash,true,"Revisado"); var lot = await f.Db.Set<LoteExistencia>().SingleAsync();
        var input = f.Input([new(2,lot.Id,"7",null)]); var id = await f.Service.SaveAsync(3,input); var line = await f.Db.Set<ConteoDetalle>().SingleAsync(); Assert.Equal(8,line.SaldoReferenciaEscalado);
        await f.Service.FinalizeAsync(3,id,1); Assert.Equal(38m,(await f.Op.StocksAsync())[2]);
        await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,f.Input([new(1,lot.Id,"7",null)]))); f.Db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,f.Input([new(2,null,"30",null),new(2,lot.Id,"7",null)])));
    }
    [Fact]
    public async Task lot_alerts_use_configured_threshold_without_exposing_cost_or_mutating_stock()
    {
        using var f = new Fixture(); var product = await f.Db.Set<Producto>().SingleAsync(x=>x.Id==2); product.ControlVencimiento=1;
        var config=await f.Db.Set<ConfiguracionNegocio>().SingleAsync();config.DiasAlertaVencimiento=7;
        var today=DateTime.UtcNow.AddHours(-6).Date;await f.Db.SaveChangesAsync();
        var receiving=f.Scope.ServiceProvider.GetRequiredService<ReceivingService>();var supplier=await receiving.SaveSupplierAsync(1,new(null,0,"Proveedor alerta",null,null,null,null,true));
        var receipt=await receiving.SaveAsync(3,new(null,0,Guid.NewGuid().ToString(),"REC-ALERTA",supplier,"DOC","2026-11-12",null,[new(2,"3","3","20",today.AddDays(3).ToString("yyyy-MM-dd"),"ALERTA-PRUEBA",null)]));
        var auth=await receiving.SubmitAsync(3,receipt,1);var hash=(await f.Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==auth)).ContenidoHash;
        await receiving.DecideAsync(1,auth,1,hash,true,"Lote revisado");var lot=await f.Db.Set<LoteExistencia>().SingleAsync();
        using var client=f.Factory.Client();await HttpTestSupport.LoginAsync(client,"inventario");
        var response=await client.GetAsync("/Lotes");var html=await response.Content.ReadAsStringAsync();Assert.Equal(HttpStatusCode.OK,response.StatusCode);Assert.Contains("Pr&#xF3;ximo a vencer",html);Assert.Contains("Existencias sin lote identificado",html);Assert.DoesNotContain("Costo",html);Assert.Equal(33m,(await f.Op.StocksAsync())[2]);
        lot.Vencimiento=today.AddDays(-1).ToString("yyyy-MM-dd");await f.Db.SaveChangesAsync();Assert.Contains("Vencido",await (await client.GetAsync("/Lotes")).Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task counted_product_keeps_its_unit_even_without_any_stock_movement()
    {
        using var f=new Fixture();var catalog=f.Scope.ServiceProvider.GetRequiredService<CatalogService>();
        var id=await catalog.SaveProductAsync(1,new(null,0,"Producto contado",1,"unidad","unidad","0",false,true));
        await f.Service.SaveAsync(3,f.Input([new(id,null,"10",null)]));
        await Assert.ThrowsAsync<BusinessException>(()=>catalog.SaveProductAsync(1,new(id,1,"Producto contado",1,"peso","lb","0",false,true)));
    }
    [Fact]
    public async Task actual_count_form_requires_physical_confirmation_and_can_finalize()
    {
        using var f=new Fixture();using var client=f.Factory.Client();await HttpTestSupport.LoginAsync(client,"inventario");
        var response=await HttpTestSupport.PostAsync(client,"/Conteos/Editar",new(){["Input.Reference"]="CON-FORM",["Input.PhysicalChecked"]="true",["Input.Lines[0].ProductId"]="2",["Input.Lines[0].Physical"]="28"});
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);var count=await f.Db.Set<Conteo>().SingleAsync();
        response=await HttpTestSupport.PostAsync(client,"/Conteos/Editar?handler=Finalize",new(){["Input.Id"]=count.Id.ToString(),["Input.Version"]="1"},"/Conteos/Editar?id="+count.Id);
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);Assert.Equal(30m,(await f.Op.StocksAsync())[2]);
    }
}
