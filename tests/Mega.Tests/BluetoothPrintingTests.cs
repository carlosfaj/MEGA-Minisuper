using Mega.Web.Services;

namespace Mega.Tests;

public sealed class BluetoothPrintingTests
{
    internal sealed class Ports:ISerialPrinterPorts
    {
        public string[] Available=["COM7"];public Exception? OpenError;public Exception? ListError;
        public Connection Device{get;}=new();public int Opens;
        public IReadOnlyList<string> Names(){if(ListError is not null)throw ListError;return Available;}
        public ISerialPrinterConnection Open(string port){Opens++;if(OpenError is not null)throw OpenError;Assert.Equal("COM7",port);return Device;}
    }
    internal sealed class Connection:ISerialPrinterConnection
    {
        public List<byte> Data{get;}=[];public bool Disposed;public Exception? WriteError;public Exception? DrainError;public Exception? CloseError;
        public int BytesToWrite{get{if(DrainError is not null)throw DrainError;return 0;}}
        public void Write(byte[] data,int offset,int count,int timeoutMilliseconds)
        {Assert.InRange(count,1,256);Assert.InRange(timeoutMilliseconds,1,30000);Data.AddRange(data.Skip(offset).Take(count));if(WriteError is not null)throw WriteError;}
        public void Dispose(){Disposed=true;if(CloseError is not null)throw CloseError;}
    }
    private static PrintDocument Document=>new(9,BluetoothPrinterTarget.ForPort("COM7"),"MEGA-9",0,ReceiptText.Build(ThermalReceiptTests.Snapshot,0,32));
    [Fact]
    public async Task successful_send_is_chunked_closed_and_does_not_claim_physical_printing()
    {
        var ports=new Ports();var transport=new BluetoothPrintTransport(ports);
        Assert.Single(transport.Printers());Assert.Equal(0,ports.Opens);
        var result=await transport.SendAsync(Document);
        Assert.Equal(ThermalReceipt.Encode(Document.Text),ports.Device.Data.ToArray());Assert.True(ports.Device.Disposed);
        Assert.StartsWith("BT-9-",result.JobId);Assert.Contains("No confirma impresión ni entrega física",result.Evidence);
    }
    [Fact]
    public async Task absent_or_malformed_port_never_opens_a_device()
    {
        var ports=new Ports{Available=[]};var transport=new BluetoothPrintTransport(ports);
        Assert.False((await Assert.ThrowsAsync<PrintDispatchException>(()=>transport.SendAsync(Document))).Uncertain);
        Assert.False((await Assert.ThrowsAsync<PrintDispatchException>(()=>transport.SendAsync(Document with{Printer="MP58-16 Bluetooth (COM7\\file)"}))).Uncertain);
        Assert.Equal(0,ports.Opens);
    }
    [Fact]
    public async Task occupied_or_disconnected_port_fails_before_any_bytes_are_sent()
    {
        var ports=new Ports{OpenError=new UnauthorizedAccessException()};
        Assert.False((await Assert.ThrowsAsync<PrintDispatchException>(()=>new BluetoothPrintTransport(ports).SendAsync(Document))).Uncertain);
        Assert.Empty(ports.Device.Data);
    }
    [Theory]
    [InlineData("write")][InlineData("drain")][InlineData("close")]
    public async Task partial_write_drain_or_close_failure_is_uncertain_and_not_retried(string phase)
    {
        var ports=new Ports();
        if(phase=="write")ports.Device.WriteError=new TimeoutException();
        if(phase=="drain")ports.Device.DrainError=new IOException();
        if(phase=="close")ports.Device.CloseError=new IOException();
        Assert.True((await Assert.ThrowsAsync<PrintDispatchException>(()=>new BluetoothPrintTransport(ports).SendAsync(Document))).Uncertain);
        Assert.Equal(1,ports.Opens);Assert.NotEmpty(ports.Device.Data);Assert.True(ports.Device.Disposed);
    }
    [Fact]
    public async Task unsupported_characters_fail_without_opening_connection()
    {
        var ports=new Ports();
        Assert.False((await Assert.ThrowsAsync<PrintDispatchException>(()=>new BluetoothPrintTransport(ports).SendAsync(Document with{Text="漢"}))).Uncertain);
        Assert.Equal(0,ports.Opens);
    }
    private sealed class WindowsFake:IPrintTransport
    {
        public int Sends;public IReadOnlyList<string> Printers()=>["Microsoft Print to PDF"];
        public Task<PrintAcceptance> SendAsync(PrintDocument document){Sends++;return Task.FromResult(new PrintAcceptance("PDF","Sin papel"));}
    }
    [Fact]
    public async Task windows_path_is_preserved_even_when_serial_discovery_fails()
    {
        var ports=new Ports{ListError=new IOException()};var windows=new WindowsFake();
        var router=new LocalPrintTransport(windows,new BluetoothPrintTransport(ports));
        Assert.Equal(new[]{"Microsoft Print to PDF"},router.Printers());
        Assert.Equal("PDF",(await router.SendAsync(Document with{Printer="Microsoft Print to PDF"})).JobId);
        Assert.Equal(1,windows.Sends);Assert.Equal(0,ports.Opens);
    }
    [Fact]
    public async Task bluetooth_error_never_falls_back_to_another_printer()
    {
        var windows=new WindowsFake();var router=new LocalPrintTransport(windows,new BluetoothPrintTransport(new Ports{OpenError=new IOException()}));
        await Assert.ThrowsAsync<PrintDispatchException>(()=>router.SendAsync(Document));Assert.Equal(0,windows.Sends);
    }
    private sealed class SlowPorts:ISerialPrinterPorts
    {
        public TaskCompletionSource<ISerialPrinterConnection> Gate{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IReadOnlyList<string> Names()=>["COM7"];
        public ISerialPrinterConnection Open(string port)=>Gate.Task.GetAwaiter().GetResult();
    }
    private sealed class LateConnection:ISerialPrinterConnection
    {
        public TaskCompletionSource Closed{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int BytesToWrite=>0;public int Writes;
        public void Write(byte[] data,int offset,int count,int timeoutMilliseconds)=>Writes++;
        public void Dispose()=>Closed.TrySetResult();
    }
    [Fact]
    public async Task slow_open_releases_dispatch_and_disposes_late_connection_without_sending()
    {
        var ports=new SlowPorts();var late=new LateConnection();
        try
        {
            var error=await Assert.ThrowsAsync<PrintDispatchException>(()=>new BluetoothPrintTransport(ports,TimeSpan.FromMilliseconds(30)).SendAsync(Document));
            Assert.False(error.Uncertain);
        }
        finally{ports.Gate.TrySetResult(late);}
        await late.Closed.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.Equal(0,late.Writes);
    }
}
