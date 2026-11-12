using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mega.Tests;
public sealed class CancellationTests
{
    private sealed class Fixture:IDisposable
    {
        public MegaFactory Factory{get;}=new();public IServiceScope Scope{get;}
        public MegaDbContext Db=>Scope.ServiceProvider.GetRequiredService<MegaDbContext>();public CancellationService Service=>Scope.ServiceProvider.GetRequiredService<CancellationService>();public Operations Op=>Scope.ServiceProvider.GetRequiredService<Operations>();
        public long Sale;
        public Fixture()=>Scope=Factory.Services.CreateScope();
        public async Task Setup(){await Op.OpenCashAsync(2,"500",null);List<LineInput> lines=[new(2,"unidad","1")];var q=await Op.QuoteAsync(2,lines);Sale=await Op.ConfirmAsync(2,new(Guid.NewGuid().ToString(),q.Hash,"50",false,lines));}
        public CancellationChange Input(bool restock=true)=>new(null,0,Guid.NewGuid().ToString(),Sale,"AN-"+Guid.NewGuid(),"Motivo ficticio de anulación","Acta ficticia",restock,restock);
        public async Task<Autorizacion> Send(CancellationChange input,long user=2){var id=await Service.SaveAsync(user,input);var a=await Service.SubmitAsync(user,id,1);Db.ChangeTracker.Clear();return await Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==a);}
        public Task<long?> Apply(Autorizacion a,long user=1)=>Service.DecideAsync(user,a.Id,a.ObjetoVersion,a.ContenidoHash,true,"Evidencia revisada");
        public void Dispose(){Scope.Dispose();Factory.Dispose();}
    }
    [Fact]
    public async Task T06_restock_and_cash_return_are_separate_atomic_and_idempotent()
    {
        using var f=new Fixture();await f.Setup();var snapshot=(await f.Db.Set<Venta>().SingleAsync()).ComprobanteSnapshot;var a=await f.Send(f.Input());Assert.Equal(29m,(await f.Op.StocksAsync())[2]);
        var op=await f.Apply(a);Assert.Equal(30m,(await f.Op.StocksAsync())[2]);Assert.Equal(53800,(await f.Op.CashAsync(2)).Expected);var r=await f.Db.Set<SolicitudAnulacion>().SingleAsync();Assert.Equal("Pendiente",r.RegularizacionEstado);
        var refund=await f.Service.RefundAsync(2,r.Id,1,true,"Entrega ficticia documentada");Assert.Equal(refund,await f.Service.RefundAsync(2,r.Id,1,true,"Reintento"));Assert.Equal(op,await f.Apply(a));f.Db.ChangeTracker.Clear();
        Assert.Equal(50000,(await f.Op.CashAsync(2)).Expected);Assert.Equal(snapshot,(await f.Db.Set<Venta>().SingleAsync()).ComprobanteSnapshot);Assert.Equal("Anulada",(await f.Db.Set<Venta>().SingleAsync()).Estado);Assert.Equal("Completada",(await f.Db.Set<SolicitudAnulacion>().SingleAsync()).RegularizacionEstado);
        Assert.Equal(1,await f.Db.Set<MovimientoInventario>().CountAsync(x=>x.MovimientoRevertidoId!=null));Assert.Equal(1,await f.Db.Set<MovimientoCaja>().CountAsync(x=>x.Tipo=="devolucion_efectivo"));
        await f.Db.Database.OpenConnectionAsync();var report=await f.Scope.ServiceProvider.GetRequiredService<DatabaseInspector>().InspectAsync((Microsoft.Data.Sqlite.SqliteConnection)f.Db.Database.GetDbConnection(),f.Db.Model);Assert.True(report.Healthy,string.Join(";",report.Errors));
    }
    [Fact]
    public async Task T08_T09_old_closed_cash_is_preserved_and_real_return_uses_current_shift()
    {
        using var f=new Fixture();await f.Setup();var old=(await f.Op.CashAsync(2)).Shift!;await f.Op.CloseCashAsync(2,old.Id,old.Version,"538",null);f.Db.ChangeTracker.Clear();var closed=Formats.Json(await f.Db.Set<TurnoCaja>().SingleAsync());var a=await f.Send(f.Input());await f.Apply(a);
        await Assert.ThrowsAsync<BusinessException>(()=>f.Service.RefundAsync(2,a.SolicitudAnulacionId!.Value,1,true,"Sin turno"));f.Db.ChangeTracker.Clear();
        await f.Op.OpenCashAsync(2,"500",null);await f.Service.RefundAsync(2,a.SolicitudAnulacionId!.Value,1,true,"Entrega desde turno nuevo");f.Db.ChangeTracker.Clear();
        Assert.Equal(closed,Formats.Json(await f.Db.Set<TurnoCaja>().SingleAsync(x=>x.Id==old.Id)));Assert.Equal(46200,(await f.Op.CashAsync(2)).Expected);
        var movement=await f.Db.Set<MovimientoCaja>().SingleAsync(x=>x.Tipo=="devolucion_efectivo");Assert.NotEqual(old.Id,movement.TurnoId);
    }
    [Fact]
    public async Task no_restock_does_not_invent_stock_and_missing_cash_evidence_does_not_invent_egress()
    {
        using var f=new Fixture();await f.Setup();await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(2,f.Input() with{PhysicalVerified=false}));
        var a=await f.Send(f.Input(false));await f.Apply(a);Assert.Equal(29m,(await f.Op.StocksAsync())[2]);
        await Assert.ThrowsAsync<BusinessException>(()=>f.Service.RefundAsync(2,a.SolicitudAnulacionId!.Value,1,false,"Acta"));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.RefundAsync(2,a.SolicitudAnulacionId!.Value,1,true,""));Assert.Equal(53800,(await f.Op.CashAsync(2)).Expected);
    }
    [Fact]
    public async Task permissions_self_approval_and_foreign_sales_are_rejected()
    {
        using var f=new Fixture();await f.Setup();await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(3,f.Input()));await Assert.ThrowsAsync<BusinessException>(()=>f.Service.SaveAsync(4,f.Input()));
        var a=await f.Send(f.Input(),1);await Assert.ThrowsAsync<BusinessException>(()=>f.Apply(a,1));await f.Apply(a,5);await Assert.ThrowsAsync<BusinessException>(()=>f.Service.RefundAsync(4,a.SolicitudAnulacionId!.Value,1,true,"Otra cuenta"));
    }
    [Fact]
    public async Task T10_edited_request_invalidates_open_review_before_any_effect()
    {
        using var f=new Fixture();await f.Setup();var input=f.Input();var a=await f.Send(input);await f.Service.SaveAsync(2,input with{Id=a.SolicitudAnulacionId,Version=1,Restock=false,PhysicalVerified=false});await Assert.ThrowsAsync<BusinessException>(()=>f.Apply(a));Assert.Equal("Confirmada",(await f.Db.Set<Venta>().SingleAsync()).Estado);Assert.Equal(29m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task rejection_or_cancellation_does_not_change_sale_cash_or_stock()
    {
        using var f=new Fixture();await f.Setup();var a=await f.Send(f.Input());await f.Service.DecideAsync(1,a.Id,1,a.ContenidoHash,false,"Evidencia insuficiente");
        var b=await f.Send(f.Input());await f.Service.CancelAsync(2,b.SolicitudAnulacionId!.Value,1,"No corresponde");await Assert.ThrowsAsync<BusinessException>(()=>f.Apply(b));Assert.Equal(29m,(await f.Op.StocksAsync())[2]);Assert.Equal(53800,(await f.Op.CashAsync(2)).Expected);Assert.Equal("Confirmada",(await f.Db.Set<Venta>().SingleAsync()).Estado);
    }
    [Fact]
    public async Task late_restock_failure_rolls_back_sale_state_authorization_and_operation()
    {
        using var f=new Fixture();await f.Setup();var a=await f.Send(f.Input());await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_cancel BEFORE INSERT ON MovimientoInventario WHEN NEW.movimientoRevertidoId IS NOT NULL BEGIN SELECT RAISE(ABORT,'Fallo prueba'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>f.Apply(a));f.Db.ChangeTracker.Clear();Assert.Equal("Confirmada",(await f.Db.Set<Venta>().SingleAsync()).Estado);Assert.Equal("pendiente",(await f.Db.Set<Autorizacion>().SingleAsync(x=>x.Id==a.Id)).Decision);Assert.Equal(0,await f.Db.Set<OperacionInventario>().CountAsync(x=>x.Tipo=="reversion"));Assert.Equal(29m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task concurrent_decisions_and_cash_retries_create_one_effect_each()
    {
        using var f=new Fixture();await f.Setup();var a=await f.Send(f.Input());
        async Task<long?> Approve(){using var s=f.Factory.Services.CreateScope();return await s.ServiceProvider.GetRequiredService<CancellationService>().DecideAsync(1,a.Id,1,a.ContenidoHash,true,"Revisado");}
        var approvals=await Task.WhenAll(Task.Run(Approve),Task.Run(Approve));Assert.Equal(approvals[0],approvals[1]);
        async Task<long> Refund(){using var s=f.Factory.Services.CreateScope();return await s.ServiceProvider.GetRequiredService<CancellationService>().RefundAsync(2,a.SolicitudAnulacionId!.Value,1,true,"Entrega");}
        var refunds=await Task.WhenAll(Task.Run(Refund),Task.Run(Refund));Assert.Equal(refunds[0],refunds[1]);Assert.Equal(50000,(await f.Op.CashAsync(2)).Expected);Assert.Equal(30m,(await f.Op.StocksAsync())[2]);
    }
    [Fact]
    public async Task reversal_uses_sale_historical_cost_after_new_receipts_change_average()
    {
        using var f=new Fixture();var original=await f.Db.Set<MovimientoInventario>().SingleAsync(x=>x.ProductoId==2);original.EstadoCosto="conocido";original.CostoUnitEscalado=18000000;original.EscalaCosto=6;await f.Db.SaveChangesAsync();await f.Setup();
        var receive=f.Scope.ServiceProvider.GetRequiredService<ReceivingService>();var supplier=await receive.SaveSupplierAsync(1,new(null,0,"Proveedor posterior",null,null,null,null,true));var r=await receive.SaveAsync(3,new(null,0,Guid.NewGuid().ToString(),"REC-POSTERIOR",supplier,"DOC","2026-11-12",null,[new(2,"10","10","30",null,null,null)]));var aid=await receive.SubmitAsync(3,r,1);var hash=(await f.Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==aid)).ContenidoHash;await receive.DecideAsync(1,aid,1,hash,true,"Revisado");
        Assert.NotEqual(18000000,(await f.Scope.ServiceProvider.GetRequiredService<ValuationService>().StateAsync(2)).Cost);await f.Apply(await f.Send(f.Input()));var reversed=await f.Db.Set<MovimientoInventario>().SingleAsync(x=>x.MovimientoRevertidoId!=null);Assert.Equal(18000000,reversed.CostoUnitEscalado);
    }
    [Fact]
    public async Task late_refund_failure_rolls_back_money_request_state_and_shift_version()
    {
        using var f=new Fixture();await f.Setup();var a=await f.Send(f.Input());await f.Apply(a);var before=(await f.Op.CashAsync(2)).Shift!.Version;
        await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_refund_audit BEFORE INSERT ON EventoAuditoria WHEN NEW.accion='Registrar devolución efectiva' BEGIN SELECT RAISE(ABORT,'Fallo auditoria prueba'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>f.Service.RefundAsync(2,a.SolicitudAnulacionId!.Value,1,true,"Entrega de prueba"));f.Db.ChangeTracker.Clear();
        Assert.Equal(53800,(await f.Op.CashAsync(2)).Expected);Assert.Equal(before,(await f.Op.CashAsync(2)).Shift!.Version);Assert.Equal(0,(await f.Db.Set<SolicitudAnulacion>().SingleAsync()).EfectivoDevueltoCentavos);Assert.Empty(await f.Db.Set<MovimientoCaja>().Where(x=>x.Tipo=="devolucion_efectivo").ToListAsync());
    }
    [Fact]
    public async Task actual_forms_review_and_refund_preserve_receipt_and_permissions()
    {
        using var f=new Fixture();await f.Setup();using var cashier=f.Factory.Client();await HttpTestSupport.LoginAsync(cashier,"cajero");var response=await HttpTestSupport.PostAsync(cashier,"/Anulaciones/Editar",new(){["Input.SaleId"]=f.Sale.ToString(),["Input.Key"]=Guid.NewGuid().ToString(),["Input.Reference"]="AN-FORM",["Input.Reason"]="Motivo ficticio",["Input.Evidence"]="Acta ficticia",["Input.Restock"]="true",["Input.PhysicalVerified"]="true"});Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        var r=await f.Db.Set<SolicitudAnulacion>().SingleAsync();var aid=await f.Service.SubmitAsync(2,r.Id,1);var a=await f.Db.Set<Autorizacion>().AsNoTracking().SingleAsync(x=>x.Id==aid);using var admin=f.Factory.Client();await HttpTestSupport.LoginAsync(admin);response=await HttpTestSupport.PostAsync(admin,"/Anulaciones/Revision?handler=Approve",new(){["Id"]=aid.ToString(),["ExpectedVersion"]="1",["ExpectedHash"]=a.ContenidoHash,["Reason"]="Evidencia revisada"},"/Anulaciones/Revision?id="+aid);Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        response=await HttpTestSupport.PostAsync(cashier,"/Anulaciones/Editar?handler=Refund",new(){["Input.Id"]=r.Id.ToString(),["Input.Version"]="1",["ActionEvidence"]="Entrega ficticia",["HandedOver"]="true"},"/Anulaciones/Editar?id="+r.Id);Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);Assert.Equal(50000,(await f.Op.CashAsync(2)).Expected);
        Assert.Contains("Esta venta fue anulada",await (await admin.GetAsync("/Comprobante/"+f.Sale)).Content.ReadAsStringAsync());using var other=f.Factory.Client();await HttpTestSupport.LoginAsync(other,"cajero2");Assert.Contains("SinPermiso",(await other.GetAsync("/Anulaciones/Editar?id="+r.Id)).Headers.Location!.ToString());
    }
}
