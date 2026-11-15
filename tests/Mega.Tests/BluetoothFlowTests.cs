using System.Net;
using Mega.Web.Data;
using Mega.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mega.Tests;

public sealed class BluetoothFlowTests
{
    private sealed class Fixture:IDisposable
    {
        public BluetoothPrintingTests.Ports Ports{get;}=new();
        public MegaFactory Factory{get;}public IServiceScope Scope{get;}
        public MegaDbContext Db=>Scope.ServiceProvider.GetRequiredService<MegaDbContext>();
        public Operations Op=>Scope.ServiceProvider.GetRequiredService<Operations>();
        public SettingsService Settings=>Scope.ServiceProvider.GetRequiredService<SettingsService>();
        public PrintDispatcher Dispatcher=>Factory.Services.GetRequiredService<PrintDispatcher>();
        public PrintQueue Queue=>Scope.ServiceProvider.GetRequiredService<PrintQueue>();
        public SaleInput Input=null!;public long Sale;
        public Fixture()
        {Factory=new(services:s=>{s.RemoveAll<ISerialPrinterPorts>();s.AddSingleton<ISerialPrinterPorts>(Ports);});Scope=Factory.Services.CreateScope();}
        public async Task Setup(bool enabled=true)
        {
            var c=await Settings.CurrentAsync();await Settings.SaveAsync(1,new(c.Id,c.Version,c.Nombre,c.Direccion,c.Telefono,c.Emisor,c.IdentificacionEmisor,c.RegimenFiscal,"500",null,BluetoothPrinterTarget.ForPort("COM7"),enabled));
            await Op.OpenCashAsync(2,"500",null);List<LineInput> lines=[new(2,"unidad","1")];var quote=await Op.QuoteAsync(2,lines);
            Input=new(Guid.NewGuid().ToString(),quote.Hash,"50",false,lines);Sale=await Op.ConfirmAsync(2,Input);Db.ChangeTracker.Clear();
        }
        public async Task<SolicitudImpresion> Job(){Db.ChangeTracker.Clear();return await Db.Set<SolicitudImpresion>().SingleAsync(x=>x.Tipo=="original");}
        public async Task AssertSaleUnchanged()
        {
            Assert.Equal(Sale,await Op.ConfirmAsync(2,Input));Assert.Equal(1,await Db.Set<Venta>().CountAsync());Assert.Equal(1,await Db.Set<Pago>().CountAsync());
            Assert.Equal(53800,(await Op.CashAsync(2)).Expected);Assert.Equal(29m,(await Op.StocksAsync())[2]);Assert.Empty(await Db.Set<EntregaComprobante>().ToListAsync());
        }
        public void Dispose(){Scope.Dispose();Factory.Dispose();}
    }
    [Fact]
    public async Task real_router_dispatches_32_column_original_and_copy_without_another_sale()
    {
        using var f=new Fixture();await f.Setup();var snapshot=(await f.Db.Set<Venta>().SingleAsync()).ComprobanteSnapshot;
        Assert.True(await f.Dispatcher.RunOnceAsync());Assert.Equal("Aceptada",(await f.Job()).Estado);
        Assert.Equal(ThermalReceipt.Encode(ReceiptText.Build(snapshot,0,32)),f.Ports.Device.Data.ToArray());
        Assert.False(await f.Dispatcher.RunOnceAsync());await f.AssertSaleUnchanged();
        f.Ports.Device.Data.Clear();var key=Guid.NewGuid().ToString();var copy=await f.Queue.CopyAsync(2,f.Sale,key,"Ensayo Bluetooth ficticio");
        Assert.Equal(copy,await f.Queue.CopyAsync(2,f.Sale,key,"Reintento"));Assert.True(await f.Dispatcher.RunOnceAsync());
        Assert.Equal(ThermalReceipt.Encode(ReceiptText.Build(snapshot,1,32)),f.Ports.Device.Data.ToArray());
        Assert.Equal(snapshot,(await f.Db.Set<Venta>().AsNoTracking().SingleAsync()).ComprobanteSnapshot);await f.AssertSaleUnchanged();
    }
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task bluetooth_failures_use_existing_queue_controls_and_never_repeat_sale(bool partial)
    {
        using var f=new Fixture();await f.Setup();
        if(partial)f.Ports.Device.WriteError=new IOException();else f.Ports.OpenError=new IOException();
        Assert.True(await f.Dispatcher.RunOnceAsync());var job=await f.Job();Assert.Equal(partial?"Incierta":"Fallida",job.Estado);
        Assert.False(await f.Dispatcher.RunOnceAsync());Assert.Equal(1,f.Ports.Opens);await f.AssertSaleUnchanged();
        if(partial)await Assert.ThrowsAsync<BusinessException>(()=>f.Queue.ResolveAsync(1,job.Id,PrintQueue.ReviewHash(job),"reintentar_mismo","Motivo","Papel revisado",true));
        else
        {
            await f.Queue.ResolveAsync(1,job.Id,PrintQueue.ReviewHash(job),"reintentar_mismo","Conexión recuperada","Papel revisado",true);
            f.Ports.OpenError=null;Assert.True(await f.Dispatcher.RunOnceAsync());Assert.Equal("Aceptada",(await f.Job()).Estado);
            Assert.Equal(2,await f.Db.Set<IntentoImpresion>().CountAsync());await f.AssertSaleUnchanged();
        }
    }
    [Fact]
    public async Task disabled_bluetooth_keeps_pending_and_never_opens_port()
    {using var f=new Fixture();await f.Setup(false);Assert.False(await f.Dispatcher.RunOnceAsync());Assert.Equal(0,f.Ports.Opens);Assert.Equal("Pendiente",(await f.Job()).Estado);await f.AssertSaleUnchanged();}
    [Fact]
    public async Task changing_current_destination_does_not_redirect_a_historical_job()
    {
        using var f=new Fixture();await f.Setup();var c=await f.Settings.CurrentAsync();f.Ports.Available=["COM7","COM8"];
        await f.Settings.SaveAsync(1,new(c.Id,c.Version,c.Nombre,c.Direccion,null,null,null,null,"500",null,BluetoothPrinterTarget.ForPort("COM8"),true));
        Assert.False(await f.Dispatcher.RunOnceAsync());Assert.Equal(0,f.Ports.Opens);Assert.Equal(BluetoothPrinterTarget.ForPort("COM7"),(await f.Job()).ImpresoraHistorica);await f.AssertSaleUnchanged();
    }
    [Fact]
    public async Task manager_can_select_bluetooth_but_unavailable_port_cannot_enable_printing()
    {
        using var f=new Fixture();using var client=f.Factory.Client();await HttpTestSupport.LoginAsync(client);
        var html=await client.GetStringAsync("/Configuracion");Assert.Contains("MP58-16 Bluetooth (COM7)",html);
        var response=await HttpTestSupport.PostAsync(client,"/Configuracion",new(){["Input.Id"]="1",["Input.Version"]="1",["Input.Name"]="MEGA prueba Bluetooth",["Input.Address"]="Dirección ficticia",["Input.UsualFund"]="500",["Input.Printer"]=BluetoothPrinterTarget.ForPort("COM7"),["Input.EnablePrint"]="true"});
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);var c=await f.Settings.CurrentAsync();Assert.Equal(1,c.ImpresionHabilitada);Assert.Equal(BluetoothPrinterTarget.ForPort("COM7"),c.ImpresoraNombre);
        await Assert.ThrowsAsync<BusinessException>(()=>f.Settings.SaveAsync(1,new(c.Id,c.Version,c.Nombre,c.Direccion,null,null,null,null,"500",null,BluetoothPrinterTarget.ForPort("COM8"),true)));
        Assert.Equal(c.Id,(await f.Settings.CurrentAsync()).Id);Assert.Equal(0,f.Ports.Opens);
    }
}
