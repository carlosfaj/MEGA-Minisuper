using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;

public sealed class ValuationTests
{
    private static ValuationMovement M(long id,long quantity,long? cost)=>new(id,quantity,0,cost is null?"desconocido":"conocido",cost,cost is null?null:6,id,1);
    [Fact]
    public void moving_average_keeps_unknown_zero_and_negative_distinct()
    {
        Assert.Equal(15000000,ValuationService.Calculate(0,[M(1,10,10000000),M(2,10,20000000)]).Cost);
        Assert.Null(ValuationService.Calculate(0,[M(1,10,null),M(2,10,20000000)]).Cost);
        Assert.Equal(0,ValuationService.Calculate(0,[M(1,10,0)]).Cost);
        Assert.Null(ValuationService.Calculate(0,[M(1,-5,null),M(2,10,20000000)]).Cost);
        Assert.Equal(20000000,ValuationService.Calculate(0,[M(1,10,null),M(2,-10,null),M(3,5,20000000)]).Cost);
        Assert.Null(ValuationService.Calculate(0,[M(1,10,10000000),M(2,-10,10000000)]).Cost);
    }
    [Fact]
    public void confirmation_order_controls_average_and_large_convex_average_does_not_overflow()
    {
        var rows=new[]{M(1,10,10000000),M(2,-10,10000000),M(3,5,30000000)};
        Assert.Equal(30000000,ValuationService.Calculate(0,rows.Reverse().ToArray()).Cost);
        Assert.Equal(long.MaxValue,ValuationService.Calculate(0,[M(1,1,long.MaxValue),M(2,1,long.MaxValue)]).Cost);
    }
    private sealed class Fixture:IDisposable
    {
        public MegaFactory Factory {get;}=new();public IServiceScope Scope {get;}
        public MegaDbContext Db=>Scope.ServiceProvider.GetRequiredService<MegaDbContext>();public Operations Op=>Scope.ServiceProvider.GetRequiredService<Operations>();
        public ReceivingService Receiving=>Scope.ServiceProvider.GetRequiredService<ReceivingService>();public ValuationService Valuation=>Scope.ServiceProvider.GetRequiredService<ValuationService>();
        public long Product,Presentation,Supplier;
        public Fixture()=>Scope=Factory.Services.CreateScope();
        public async Task Setup(bool lots=false)
        {
            var c=Scope.ServiceProvider.GetRequiredService<CatalogService>();Product=await c.SaveProductAsync(1,new(null,0,"Producto valoración",1,"unidad","unidad","0",lots,true));
            Presentation=await c.SavePresentationAsync(1,new(null,Product,1,0,"Unidad","1",true));
            var version=(await Db.Set<Producto>().AsNoTracking().SingleAsync(x=>x.Id==Product)).Version;
            await c.SetPriceAsync(1,Product,version,Presentation,"30",null);Supplier=await Receiving.SaveSupplierAsync(1,new(null,0,"Proveedor valoración",null,null,null,null,true));await Op.OpenCashAsync(2,"500",null);
        }
        public async Task Receive(string qty,string? cost,string? lot=null,string? expiry=null)
        {
            var r=await Receiving.SaveAsync(3,new(null,0,Guid.NewGuid().ToString(),"REC-"+Guid.NewGuid(),Supplier,"DOC-"+Guid.NewGuid(),"2026-11-12",null,[new(Presentation,qty,qty,cost,expiry,lot,null)]));
            var a=await Receiving.SubmitAsync(3,r,1);var hash=(await Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==a)).ContenidoHash;await Receiving.DecideAsync(1,a,1,hash,true,"Entrada revisada");
        }
        public async Task<long> Sell(string qty)
        {List<LineInput> lines=[new(Presentation,"unidad",qty)];var q=await Op.QuoteAsync(2,lines);return await Op.ConfirmAsync(2,new(Guid.NewGuid().ToString(),q.Hash,"500",false,lines));}
        public void Dispose(){Scope.Dispose();Factory.Dispose();}
    }
    [Fact]
    public async Task received_known_cost_sells_at_moving_average_and_preserves_historical_cost()
    {
        using var f=new Fixture();await f.Setup();await f.Receive("10","10");await f.Receive("10","20");var sale=await f.Sell("2");
        f.Db.ChangeTracker.Clear();var detail=await f.Db.Set<VentaDetalle>().SingleAsync(x=>x.VentaId==sale);Assert.Equal(15000000,detail.CostoUnitEscalado);Assert.Equal("conocido",detail.EstadoCosto);
        var reference=detail.CostoReferenciaId;await f.Receive("2","30");f.Db.ChangeTracker.Clear();
        Assert.Equal(16500000,(await f.Valuation.StateAsync(f.Product)).Cost);detail=await f.Db.Set<VentaDetalle>().SingleAsync(x=>x.VentaId==sale);Assert.Equal(15000000,detail.CostoUnitEscalado);Assert.Equal(reference,detail.CostoReferenciaId);
    }
    [Fact]
    public async Task unknown_initial_stock_mixed_with_known_entry_remains_unknown_in_sale()
    {
        using var f=new Fixture();await f.Setup();var requests=f.Scope.ServiceProvider.GetRequiredService<InventoryRequestService>();
        var r=await requests.SaveAsync(3,new(null,0,Guid.NewGuid().ToString(),"CARGA-BASE","carga",null,"Base sin costo verificable","Acta ficticia",[new(f.Product,"5",null,null,null,null)]));
        var a=await requests.SubmitAsync(3,r,1);var hash=(await f.Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==a)).ContenidoHash;await requests.DecideAsync(1,a,1,hash,true,"Revisado");
        await f.Receive("5","10");var sale=await f.Sell("1");var detail=await f.Db.Set<VentaDetalle>().SingleAsync(x=>x.VentaId==sale);Assert.Null(detail.CostoUnitEscalado);Assert.Equal("desconocido",detail.EstadoCosto);
    }
    [Fact]
    public async Task expiry_order_splits_a_sale_across_lots_without_losing_cost_or_quantity()
    {
        using var f=new Fixture();await f.Setup(true);await f.Receive("3","8","TARDIO","2030-01-01");await f.Receive("4","12","TEMPRANO","2029-01-01");
        List<LineInput> lines=[new(f.Presentation,"unidad","5")];var q=await f.Op.QuoteAsync(2,lines);Assert.Equal(2,q.Lines[0].Allocations.Count);
        var early=await f.Db.Set<LoteExistencia>().SingleAsync(x=>x.Referencia=="TEMPRANO");Assert.Equal(early.Id,q.Lines[0].Allocations[0].LotId);Assert.Equal(4,q.Lines[0].Allocations[0].Quantity);
        var sale=await f.Op.ConfirmAsync(2,new(Guid.NewGuid().ToString(),q.Hash,"500",false,lines));f.Db.ChangeTracker.Clear();
        var detail=await f.Db.Set<VentaDetalle>().SingleAsync(x=>x.VentaId==sale);var movements=await f.Db.Set<MovimientoInventario>().Where(x=>x.VentaDetalleId==detail.Id).ToListAsync();Assert.Equal(2,movements.Count);Assert.Equal(-5,movements.Sum(x=>x.CantidadEscalada));Assert.All(movements,x=>Assert.Equal(10285714,x.CostoUnitEscalado));Assert.Equal(2m,(await f.Op.StocksAsync())[f.Product]);
        await f.Db.Database.OpenConnectionAsync();var report=await f.Scope.ServiceProvider.GetRequiredService<DatabaseInspector>().InspectAsync((Microsoft.Data.Sqlite.SqliteConnection)f.Db.Database.GetDbConnection(),f.Db.Model);Assert.True(report.Healthy,string.Join(";",report.Errors));
    }
    [Fact]
    public async Task expired_or_unidentified_lots_cannot_be_sold_and_cart_cannot_overallocate()
    {
        using var f=new Fixture();await f.Setup(true);await f.Receive("3","10","LOTE","2030-01-01");
        await Assert.ThrowsAsync<BusinessException>(()=>f.Op.QuoteAsync(2,[new(f.Presentation,"unidad","2"),new(f.Presentation,"unidad","2")]));
        var lot=await f.Db.Set<LoteExistencia>().SingleAsync();lot.Vencimiento="2020-01-01";await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessException>(()=>f.Op.QuoteAsync(2,[new(f.Presentation,"unidad","1")]));Assert.Empty(await f.Db.Set<Venta>().ToListAsync());
    }
    [Fact]
    public async Task changed_cost_or_lot_allocation_requires_requote_before_charging()
    {
        using var f=new Fixture();await f.Setup(true);await f.Receive("3","10","A","2030-01-01");List<LineInput> lines=[new(f.Presentation,"unidad","1")];var q=await f.Op.QuoteAsync(2,lines);
        await f.Receive("3","20","B","2029-01-01");await Assert.ThrowsAsync<BusinessException>(()=>f.Op.ConfirmAsync(2,new(Guid.NewGuid().ToString(),q.Hash,"50",false,lines)));Assert.Empty(await f.Db.Set<Venta>().ToListAsync());
    }
}
