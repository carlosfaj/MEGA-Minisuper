using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;
public sealed class IntegratedInventoryTests
{
    private sealed class Fixture:IDisposable
    {
        public MegaFactory Factory {get;}=new();public IServiceScope Scope {get;}
        public MegaDbContext Db=>Scope.ServiceProvider.GetRequiredService<MegaDbContext>();public InventoryRequestService Service=>Scope.ServiceProvider.GetRequiredService<InventoryRequestService>();
        public long Product,Lot;
        public Fixture()=>Scope=Factory.Services.CreateScope();
        public InventoryChange Input(string type,string qty="3",string? cost=null,long? lot=null,string? newLot=null)=>new(null,0,Guid.NewGuid().ToString(),"REQ-"+Guid.NewGuid(),type,null,"Motivo ficticio","Acta ficticia",[new(Product,qty,cost,type=="salida_justificada"?"dano":null,null,null,lot,newLot,newLot is null?null:"2030-01-01")]);
        public async Task<Autorizacion> Send(InventoryChange input)
        {var id=await Service.SaveAsync(3,input);var a=await Service.SubmitAsync(3,id,1);Db.ChangeTracker.Clear();return await Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==a);}
        public Task<long?> Apply(Autorizacion a)=>Service.DecideAsync(1,a.Id,a.ObjetoVersion,a.ContenidoHash,true,"Contenido revisado");
        public async Task Setup(bool lots=true)
        {
            Product=await Scope.ServiceProvider.GetRequiredService<CatalogService>().SaveProductAsync(1,new(null,0,"Producto integrado",1,"unidad","unidad","0",lots,true));
            var a=await Send(Input("carga","10","20",newLot:lots?"LOTE-BASE":null));await Apply(a);
            if(lots)Lot=(await Db.Set<LoteExistencia>().SingleAsync()).Id;
        }
        public void Dispose(){Scope.Dispose();Factory.Dispose();}
    }
    [Fact]
    public async Task load_with_new_lot_has_no_stock_until_approved_and_known_output_keeps_cost()
    {
        using var f=new Fixture();await f.Setup();var a=await f.Send(f.Input("salida_justificada","2",lot:f.Lot));
        var d=await f.Db.Set<SolicitudInventarioDetalle>().SingleAsync(x=>x.SolicitudId==a.SolicitudInventarioId);Assert.Equal(20000000,d.CostoUnitEscalado);Assert.Equal(f.Lot,d.LoteId);
        await f.Apply(a);f.Db.ChangeTracker.Clear();var m=await f.Db.Set<MovimientoInventario>().SingleAsync(x=>x.ProductoId==f.Product&&x.CantidadEscalada<0);Assert.Equal(f.Lot,m.LoteId);Assert.Equal(20000000,m.CostoUnitEscalado);
    }
    [Fact]
    public async Task counted_lot_adjustment_preserves_scope_and_known_historical_cost()
    {
        using var f=new Fixture();await f.Setup();var counting=f.Scope.ServiceProvider.GetRequiredService<CountingService>();var c=await counting.SaveAsync(3,new(null,0,"CON-LOTE",null,true,[new(f.Product,f.Lot,"8",null)]));await counting.FinalizeAsync(3,c,1);
        var a=await f.Send(new(null,0,Guid.NewGuid().ToString(),"AJ-LOTE","ajuste",c,"Diferencia física","Acta",[]));await f.Apply(a);f.Db.ChangeTracker.Clear();
        var m=await f.Db.Set<MovimientoInventario>().SingleAsync(x=>x.ProductoId==f.Product&&x.CantidadEscalada<0);Assert.Equal(-2,m.CantidadEscalada);Assert.Equal(f.Lot,m.LoteId);Assert.Equal(20000000,m.CostoUnitEscalado);
    }
    [Fact]
    public async Task total_count_cannot_guess_lot_and_unidentified_controlled_load_is_rejected()
    {
        using var f=new Fixture();await f.Setup();await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,f.Input("carga","1","20")));
        var counting=f.Scope.ServiceProvider.GetRequiredService<CountingService>();var c=await counting.SaveAsync(3,new(null,0,"CON-TOTAL",null,true,[new(f.Product,null,"8",null)]));await counting.FinalizeAsync(3,c,1);
        await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,new(null,0,Guid.NewGuid().ToString(),"AJ-TOTAL","ajuste",c,"Motivo","Acta",[])));
    }
    [Fact]
    public async Task changed_average_after_preparation_requires_saving_a_new_cost_version()
    {
        using var f=new Fixture();await f.Setup(false);var draft=f.Input("salida_justificada","1");var id=await f.Service.SaveAsync(3,draft);
        await f.Apply(await f.Send(f.Input("carga","10","40")));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SubmitAsync(3,id,1));f.Db.ChangeTracker.Clear();
        await f.Service.SaveAsync(3,draft with{Id=id,Version=1});var a=await f.Service.SubmitAsync(3,id,2);var hash=(await f.Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==a)).ContenidoHash;await f.Service.DecideAsync(1,a,2,hash,true,"Revisado");
        var m=await f.Db.Set<MovimientoInventario>().SingleAsync(x=>x.ProductoId==f.Product&&x.CantidadEscalada<0);Assert.Equal(30000000,m.CostoUnitEscalado);
    }
    [Fact]
    public async Task invalid_lot_and_insufficient_lot_stock_cannot_apply()
    {
        using var f=new Fixture();await f.Setup();await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,f.Input("salida_justificada","1",lot:99999)));
        var id=await f.Service.SaveAsync(3,f.Input("salida_justificada","11",lot:f.Lot));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SubmitAsync(3,id,1));
        Assert.Equal(10,await f.Db.Set<MovimientoInventario>().Where(x=>x.ProductoId==f.Product).SumAsync(x=>x.CantidadEscalada));
    }
}
