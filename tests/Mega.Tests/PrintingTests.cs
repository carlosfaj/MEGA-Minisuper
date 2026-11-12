using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mega.Tests;
public sealed class PrintingTests
{
    private sealed class Transport:IPrintTransport
    {
        public List<PrintDocument> Documents{get;}=[];public bool? Failure;public int Delay;
        public IReadOnlyList<string> Printers()=>["Impresora ficticia"];
        public async Task<PrintAcceptance> SendAsync(PrintDocument document){Documents.Add(document);if(Delay>0)await Task.Delay(Delay);if(Failure is bool uncertain)throw new PrintDispatchException("Fallo ficticio de impresora",uncertain);return new("JOB-"+document.RequestId,"Aceptación ficticia, sin papel real");}
    }
    private sealed class Fixture:IDisposable
    {
        public Transport Printer{get;}=new();public MegaFactory Factory{get;}public IServiceScope Scope{get;}
        public MegaDbContext Db=>Scope.ServiceProvider.GetRequiredService<MegaDbContext>();public Operations Op=>Scope.ServiceProvider.GetRequiredService<Operations>();public PrintQueue Queue=>Scope.ServiceProvider.GetRequiredService<PrintQueue>();public PrintDispatcher Dispatcher=>Factory.Services.GetRequiredService<PrintDispatcher>();
        public long Sale;public SaleInput Input=null!;
        public Fixture(){Factory=new(services:s=>{s.RemoveAll<IPrintTransport>();s.AddSingleton<IPrintTransport>(Printer);});Scope=Factory.Services.CreateScope();}
        public async Task Setup(bool enabled=true)
        {
            var settings=Scope.ServiceProvider.GetRequiredService<SettingsService>();var c=await settings.CurrentAsync();await settings.SaveAsync(1,new(c.Id,c.Version,c.Nombre,c.Direccion,c.Telefono,c.Emisor,c.IdentificacionEmisor,c.RegimenFiscal,"500",null,"Impresora ficticia",enabled));
            await Op.OpenCashAsync(2,"500",null);List<LineInput> lines=[new(2,"unidad","1")];var q=await Op.QuoteAsync(2,lines);Input=new(Guid.NewGuid().ToString(),q.Hash,"50",false,lines);Sale=await Op.ConfirmAsync(2,Input);Db.ChangeTracker.Clear();
        }
        public async Task<SolicitudImpresion> Job(){Db.ChangeTracker.Clear();return await Db.Set<SolicitudImpresion>().SingleAsync(x=>x.Tipo=="original");}
        public void Dispose(){Scope.Dispose();Factory.Dispose();}
    }
    [Fact]
    public async Task disabled_agent_keeps_pending_sale_and_does_not_dispatch()
    {using var f=new Fixture();await f.Setup(false);Assert.False(await f.Dispatcher.RunOnceAsync());Assert.Empty(f.Printer.Documents);Assert.Equal("Pendiente",(await f.Job()).Estado);Assert.Equal(53800,(await f.Op.CashAsync(2)).Expected);}
    [Fact]
    public async Task T07_known_printer_failure_never_repeats_sale_or_payment_and_requires_retry()
    {
        using var f=new Fixture();await f.Setup();f.Printer.Failure=false;Assert.True(await f.Dispatcher.RunOnceAsync());var job=await f.Job();Assert.Equal("Fallida",job.Estado);Assert.Equal("no_aceptada",(await f.Db.Set<IntentoImpresion>().SingleAsync()).Resultado);
        Assert.False(await f.Dispatcher.RunOnceAsync());Assert.Equal(f.Sale,await f.Op.ConfirmAsync(2,f.Input));Assert.Equal(1,await f.Db.Set<Venta>().CountAsync());Assert.Equal(1,await f.Db.Set<Pago>().CountAsync());Assert.Equal(53800,(await f.Op.CashAsync(2)).Expected);
        await f.Queue.ResolveAsync(1,job.Id,PrintQueue.ReviewHash(job),"reintentar_mismo","Equipo disponible","Cola comprobada",true);f.Printer.Failure=null;Assert.True(await f.Dispatcher.RunOnceAsync());Assert.Equal("Aceptada",(await f.Job()).Estado);Assert.Equal(2,await f.Db.Set<IntentoImpresion>().CountAsync());
    }
    [Fact]
    public async Task uncertain_result_is_never_automatically_resent_or_blindly_retried()
    {
        using var f=new Fixture();await f.Setup();f.Printer.Failure=true;await f.Dispatcher.RunOnceAsync();var job=await f.Job();Assert.Equal("Incierta",job.Estado);Assert.False(await f.Dispatcher.RunOnceAsync());
        await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.CopyAsync(2,f.Sale,Guid.NewGuid().ToString(),"Copia"));await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.ResolveAsync(1,job.Id,PrintQueue.ReviewHash(job),"reintentar_mismo","Motivo","Evidencia",true));
        await f.Queue.ResolveAsync(1,job.Id,PrintQueue.ReviewHash(job),"emitir_copia","Duplicado explícito aceptado","Cola revisada",true);Assert.Equal("Resuelta",(await f.Job()).Estado);Assert.Equal(1,await f.Db.Set<SolicitudImpresion>().CountAsync(x=>x.Tipo=="copia"));Assert.Single(f.Printer.Documents);
    }
    [Fact]
    public async Task expired_lease_becomes_uncertain_and_stale_completion_cannot_overwrite_resolution()
    {
        using var f=new Fixture();await f.Setup();var lease=await f.Queue.ClaimAsync();Assert.NotNull(lease);var job=await f.Job();job.LeaseHasta="2020-01-01T00:00:00.000Z";await f.Db.SaveChangesAsync();Assert.Null(await f.Queue.ClaimAsync());job=await f.Job();Assert.Equal("Incierta",job.Estado);Assert.Equal("incierta",(await f.Db.Set<IntentoImpresion>().SingleAsync()).Resultado);
        await f.Queue.ResolveAsync(1,job.Id,PrintQueue.ReviewHash(job),"no_reenviar","No duplicar","Evidencia",true);await f.Queue.FinishAsync(lease!,new("JOB-TARDIO","Resultado tardío"),null);Assert.Equal("Resuelta",(await f.Job()).Estado);Assert.Empty(f.Printer.Documents);
    }
    [Fact]
    public async Task concurrent_dispatchers_accept_exactly_one_attempt()
    {using var f=new Fixture();await f.Setup();f.Printer.Delay=80;await Task.WhenAll(f.Dispatcher.RunOnceAsync(),f.Dispatcher.RunOnceAsync());Assert.Single(f.Printer.Documents);Assert.Equal("Aceptada",(await f.Job()).Estado);Assert.Equal(1,await f.Db.Set<IntentoImpresion>().CountAsync());}
    [Fact]
    public async Task copy_is_idempotent_keeps_snapshot_folio_totals_and_does_not_charge()
    {
        using var f=new Fixture();await f.Setup();await f.Dispatcher.RunOnceAsync();var snapshot=(await f.Db.Set<Venta>().SingleAsync()).ComprobanteSnapshot;var key=Guid.NewGuid().ToString();var id=await f.Queue.CopyAsync(2,f.Sale,key,"Cliente solicita copia");Assert.Equal(id,await f.Queue.CopyAsync(2,f.Sale,key,"Reintento"));await f.Dispatcher.RunOnceAsync();
        Assert.Equal(2,f.Printer.Documents.Count);Assert.Contains("COPIA #1",f.Printer.Documents[1].Text);Assert.Contains(f.Printer.Documents[0].Folio,f.Printer.Documents[1].Text);Assert.Equal(snapshot,(await f.Db.Set<Venta>().SingleAsync()).ComprobanteSnapshot);Assert.Equal(1,await f.Db.Set<Pago>().CountAsync());Assert.Equal(1,await f.Db.Set<MovimientoCaja>().CountAsync());
    }
    [Fact]
    public async Task windows_acceptance_does_not_invent_physical_delivery()
    {
        using var f=new Fixture();await f.Setup();await f.Dispatcher.RunOnceAsync();var job=await f.Job();Assert.Empty(await f.Db.Set<EntregaComprobante>().ToListAsync());await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.DeliverAsync(2,job.Id,"Evidencia",false));var delivery=await f.Queue.DeliverAsync(2,job.Id,"Entrega ficticia",true);Assert.Equal(delivery,await f.Queue.DeliverAsync(2,job.Id,"Reintento",true));Assert.Equal(1,await f.Db.Set<EntregaComprobante>().CountAsync());
    }
    [Fact]
    public async Task contingency_is_managed_and_stale_review_is_rejected()
    {
        using var f=new Fixture();await f.Setup(false);var job=await f.Job();await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.ResolveAsync(2,job.Id,PrintQueue.ReviewHash(job),"contingencia_manual","Motivo","Acta",true));await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.ResolveAsync(1,job.Id,PrintQueue.ReviewHash(job),"contingencia_manual","Motivo","Acta",false));
        await f.Queue.ResolveAsync(1,job.Id,PrintQueue.ReviewHash(job),"contingencia_manual","Manual entregado","Acta ficticia",true);await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.ResolveAsync(1,job.Id,PrintQueue.ReviewHash(job),"emitir_copia","Motivo","Acta",true));f.Db.ChangeTracker.Clear();var delivery=await f.Db.Set<EntregaComprobante>().SingleAsync();Assert.Equal("manual_contingencia",delivery.Tipo);Assert.Equal(1,delivery.AutorizadoPor);Assert.Equal("Resuelta",(await f.Job()).Estado);Assert.Empty(f.Printer.Documents);
    }
    [Fact]
    public async Task late_contingency_failure_rolls_back_resolution_and_delivery()
    {
        using var f=new Fixture();await f.Setup(false);var job=await f.Job();await f.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_delivery BEFORE INSERT ON EntregaComprobante BEGIN SELECT RAISE(ABORT,'Fallo tardio entrega'); END;");await Assert.ThrowsAsync<DbUpdateException>(()=>f.Queue.ResolveAsync(1,job.Id,PrintQueue.ReviewHash(job),"contingencia_manual","Motivo","Acta",true));Assert.Equal("Pendiente",(await f.Job()).Estado);Assert.Empty(await f.Db.Set<EntregaComprobante>().ToListAsync());
    }
    [Fact]
    public async Task foreign_closed_and_inventory_accounts_cannot_manage_cashier_receipt()
    {
        using var f=new Fixture();await f.Setup();await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.RequireSaleAsync(4,f.Sale));await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.RequireSaleAsync(3,f.Sale));var turn=(await f.Op.CashAsync(2)).Shift!;await f.Op.CloseCashAsync(2,turn.Id,turn.Version,"538",null);await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.RequireSaleAsync(2,f.Sale));Assert.Equal(f.Sale,(await f.Queue.RequireSaleAsync(1,f.Sale)).Id);
        using var client=f.Factory.Client();await HttpTestSupport.LoginAsync(client,"inventario");Assert.Contains("SinPermiso",(await client.GetAsync("/Impresiones")).Headers.Location!.ToString());
    }
    [Fact]
    public async Task printer_setting_rejects_unknown_device_without_changing_current_version()
    {
        using var f=new Fixture();var service=f.Scope.ServiceProvider.GetRequiredService<SettingsService>();var c=await service.CurrentAsync();await Assert.ThrowsAsync<BusinessException>(()=>service.SaveAsync(1,new(c.Id,c.Version,c.Nombre,c.Direccion,null,null,null,null,"500",null,"No instalada",true)));Assert.Equal(c.Id,(await service.CurrentAsync()).Id);
    }
    [Fact]
    public async Task altered_job_hash_is_not_sent_to_printer()
    {
        using var f=new Fixture();await f.Setup();var job=await f.Job();job.ContenidoHash=new('a',64);await f.Db.SaveChangesAsync();Assert.False(await f.Dispatcher.RunOnceAsync());Assert.Equal("Fallida",(await f.Job()).Estado);Assert.Empty(f.Printer.Documents);Assert.Empty(await f.Db.Set<IntentoImpresion>().ToListAsync());
    }
    [Fact]
    public async Task cancelled_sale_pending_original_is_paused_before_spooling()
    {
        using var f=new Fixture();await f.Setup();var service=f.Scope.ServiceProvider.GetRequiredService<CancellationService>();var id=await service.SaveAsync(2,new(null,0,Guid.NewGuid().ToString(),f.Sale,"AN-TEST","Motivo ficticio","Acta ficticia",false,false));var authorization=await service.SubmitAsync(2,id,1);var review=await f.Db.Set<Autorizacion>().SingleAsync(x=>x.Id==authorization);await service.DecideAsync(1,review.Id,review.ObjetoVersion,review.ContenidoHash,true,"Revisado");Assert.False(await f.Dispatcher.RunOnceAsync());Assert.Equal("Fallida",(await f.Job()).Estado);Assert.Empty(f.Printer.Documents);
    }
}
