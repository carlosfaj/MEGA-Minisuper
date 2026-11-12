using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;

public sealed class InventoryRequestTests
{
    private sealed class Fixture:IDisposable
    {
        public MegaFactory Factory {get;}=new();public IServiceScope Scope {get;}
        public MegaDbContext Db=>Scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        public InventoryRequestService Service=>Scope.ServiceProvider.GetRequiredService<InventoryRequestService>();
        public Operations Op=>Scope.ServiceProvider.GetRequiredService<Operations>();
        public Fixture()=>Scope=Factory.Services.CreateScope();
        public InventoryChange Input(string type="carga",List<InventoryLine>? lines=null)=>new(null,0,Guid.NewGuid().ToString(),"INV-"+Guid.NewGuid().ToString("N"),type,null,"Motivo de prueba ficticia","Acta ficticia de prueba",lines??[new(2,"3",null,type=="salida_justificada"?"dano":null,null,null)]);
        public async Task<Autorizacion> Send(InventoryChange input,long user=3)
        {var id=await Service.SaveAsync(user,input);var auth=await Service.SubmitAsync(user,id,1);Db.ChangeTracker.Clear();return await Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==auth);}
        public Task<long?> Apply(Autorizacion a,long user=1)=>Service.DecideAsync(user,a.Id,a.ObjetoVersion,a.ContenidoHash,true,"Contenido y evidencia revisados");
        public async Task Sale()
        {await Op.OpenCashAsync(2,"500",null);List<LineInput> lines=[new(2,"unidad","1")];var q=await Op.QuoteAsync(2,lines);await Op.ConfirmAsync(2,new(Guid.NewGuid().ToString(),q.Hash,"50",false,lines));}
        public async Task<long> Count()
        {var s=Scope.ServiceProvider.GetRequiredService<CountingService>();var id=await s.SaveAsync(3,new(null,0,"CON-"+Guid.NewGuid(),null,true,[new(2,null,"28",null)]));await s.FinalizeAsync(3,id,1);return id;}
        public void Dispose(){Scope.Dispose();Factory.Dispose();}
    }
    [Fact]
    public async Task T04_count_adjustment_requires_review_and_applies_exact_difference_once()
    {
        using var f=new Fixture();var count=await f.Count();var input=f.Input("ajuste",[]) with{CountId=count};var auth=await f.Send(input);
        Assert.Equal(30m,(await f.Op.StocksAsync())[2]);var detail=await f.Db.Set<SolicitudInventarioDetalle>().SingleAsync(x=>x.SolicitudId==auth.SolicitudInventarioId);Assert.Equal(-2,detail.CantidadEscalada);Assert.Null(detail.CostoUnitEscalado);
        var op=await f.Apply(auth);Assert.Equal(op,await f.Apply(auth));Assert.Equal(28m,(await f.Op.StocksAsync())[2]);f.Db.ChangeTracker.Clear();
        Assert.Equal(1,await f.Db.Set<OperacionInventario>().CountAsync(x=>x.Tipo=="ajuste"));Assert.Equal("Finalizado",(await f.Db.Set<Conteo>().SingleAsync()).Estado);
        await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,f.Input("ajuste",[]) with{CountId=count}));
    }
    [Fact]
    public async Task T10_edit_invalidates_old_inventory_approval()
    {
        using var f=new Fixture();var input=f.Input();var auth=await f.Send(input);
        await f.Service.SaveAsync(3,input with{Id=auth.SolicitudInventarioId,Version=1,Lines=[new(2,"5",null,null,null,null)]});
        await Assert.ThrowsAsync<BusinessException>(()=>f.Apply(auth));Assert.Equal(30m,(await f.Op.StocksAsync())[2]);
        var next=await f.Service.SubmitAsync(3,auth.SolicitudInventarioId!.Value,2);f.Db.ChangeTracker.Clear();var current=await f.Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==next);await f.Apply(current);Assert.Equal(35m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task stock_changed_after_review_rejects_decision_and_requires_a_new_version()
    {
        using var f=new Fixture();var input=f.Input("salida_justificada");var auth=await f.Send(input);await f.Sale();
        await Assert.ThrowsAsync<BusinessException>(()=>f.Apply(auth));Assert.Equal(29m,(await f.Op.StocksAsync())[2]);f.Db.ChangeTracker.Clear();
        await f.Service.SaveAsync(3,input with{Id=auth.SolicitudInventarioId,Version=1});var next=await f.Service.SubmitAsync(3,auth.SolicitudInventarioId!.Value,2);var a=await f.Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==next);await f.Apply(a);Assert.Equal(26m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task movements_after_finalized_count_prevent_preparing_an_old_adjustment()
    {
        using var f=new Fixture();var count=await f.Count();await f.Sale();await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,f.Input("ajuste",[]) with{CountId=count}));Assert.Equal(29m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task loads_preserve_declared_known_cost_and_never_zero_unknown_cost()
    {
        using var f=new Fixture();var auth=await f.Send(f.Input());await f.Apply(auth);f.Db.ChangeTracker.Clear();
        var unknown=await f.Db.Set<MovimientoInventario>().SingleAsync(x=>x.SolicitudInventarioDetalleId!=null&&x.OperacionId!=1);Assert.Equal("desconocido",unknown.EstadoCosto);Assert.Null(unknown.CostoUnitEscalado);
        auth=await f.Send(f.Input() with{Lines=[new(6,"2","10.123456",null,null,null)]});await f.Apply(auth);f.Db.ChangeTracker.Clear();
        var known=await f.Db.Set<MovimientoInventario>().SingleAsync(x=>x.ProductoId==6&&x.EstadoCosto=="conocido");Assert.Equal(10123456,known.CostoUnitEscalado);
        await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,f.Input("salida_justificada",[new(6,"1",null,"perdida",null,null)])));
    }
    [Theory]
    [InlineData(2)] [InlineData(3)]
    public async Task only_other_management_account_can_apply(long user)
    {
        using var f=new Fixture();var auth=await f.Send(f.Input());await Assert.ThrowsAsync<BusinessException>(()=>f.Apply(auth,user));Assert.Equal(30m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task manager_cannot_approve_own_request_and_cancellation_revokes_review()
    {
        using var f=new Fixture();var auth=await f.Send(f.Input(),1);await Assert.ThrowsAsync<BusinessException>(()=>f.Apply(auth,1));
        await f.Service.CancelAsync(1,auth.SolicitudInventarioId!.Value,1,"No corresponde proceder");await Assert.ThrowsAsync<BusinessException>(()=>f.Apply(auth,5));Assert.Equal(30m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task rejection_has_no_effect_and_negative_stock_or_missing_cause_is_blocked()
    {
        using var f=new Fixture();var input=f.Input("salida_justificada");var auth=await f.Send(input);await f.Service.DecideAsync(1,auth.Id,1,auth.ContenidoHash,false,"Evidencia insuficiente");Assert.Equal(30m,(await f.Op.StocksAsync())[2]);
        await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,f.Input("salida_justificada",[new(2,"1",null,null,null,null)])));f.Db.ChangeTracker.Clear();
        var id=await f.Service.SaveAsync(3,f.Input("salida_justificada",[new(2,"31",null,"perdida",null,null)]));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SubmitAsync(3,id,1));
        await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,f.Input("salida_justificada",[new(2,"1",null,"devolucion_proveedor",null,null)])));
    }
    [Fact]
    public async Task late_failure_rolls_back_approval_and_all_inventory_effects()
    {
        using var f=new Fixture();var auth=await f.Send(f.Input());
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_request BEFORE INSERT ON MovimientoInventario WHEN NEW.operacionId>1 BEGIN SELECT RAISE(ABORT,'Prueba fallo tardio'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>f.Apply(auth));f.Db.ChangeTracker.Clear();Assert.Equal("pendiente",(await f.Db.Set<Autorizacion>().SingleAsync(x=>x.Id==auth.Id)).Decision);Assert.Equal("Pendiente",(await f.Db.Set<SolicitudInventario>().SingleAsync(x=>x.Id==auth.SolicitudInventarioId)).Estado);Assert.Equal(30m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task concurrent_retries_apply_one_load()
    {
        using var f=new Fixture();var auth=await f.Send(f.Input());
        async Task<long?> Apply(){using var s=f.Factory.Services.CreateScope();return await s.ServiceProvider.GetRequiredService<InventoryRequestService>().DecideAsync(1,auth.Id,1,auth.ContenidoHash,true,"Revisado");}
        var ids=await Task.WhenAll(Task.Run(Apply),Task.Run(Apply));Assert.Equal(ids[0],ids[1]);Assert.Equal(33m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task http_preparation_and_review_form_use_antiforgery_and_seed_history_is_readable()
    {
        using var f=new Fixture();using var inventory=f.Factory.Client();await HttpTestSupport.LoginAsync(inventory,"inventario");
        var seed=await inventory.GetAsync("/Solicitudes/Revision?id=1");Assert.Equal(HttpStatusCode.OK,seed.StatusCode);Assert.Contains("Registro inicial de demostraci&#xF3;n",await seed.Content.ReadAsStringAsync());
        var response=await HttpTestSupport.PostAsync(inventory,"/Solicitudes/Editar",new(){["Input.Key"]=Guid.NewGuid().ToString(),["Input.Type"]="carga",["Input.Reference"]="CARGA-FORM",["Input.Reason"]="Carga ficticia",["Input.Evidence"]="Acta ficticia",["Input.Lines[0].ProductId"]="2",["Input.Lines[0].Quantity"]="3"});Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        var request=await f.Db.Set<SolicitudInventario>().SingleAsync(x=>x.Referencia=="CARGA-FORM");var id=await f.Service.SubmitAsync(3,request.Id,1);var auth=await f.Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==id);
        using var admin=f.Factory.Client();await HttpTestSupport.LoginAsync(admin);response=await HttpTestSupport.PostAsync(admin,"/Solicitudes/Revision?handler=Approve",new(){["Id"]=id.ToString(),["ExpectedVersion"]="1",["ExpectedHash"]=auth.ContenidoHash,["Reason"]="Revisado por formulario"},"/Solicitudes/Revision?id="+id);Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);Assert.Equal(33m,(await f.Op.StocksAsync())[2]);
    }
}
