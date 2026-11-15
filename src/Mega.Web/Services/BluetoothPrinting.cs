using System.Diagnostics;
using System.IO.Ports;

namespace Mega.Web.Services;

public interface ISerialPrinterPorts
{
    IReadOnlyList<string> Names();
    ISerialPrinterConnection Open(string port);
}
public interface ISerialPrinterConnection:IDisposable
{
    int BytesToWrite{get;}
    void Write(byte[] data,int offset,int count,int timeoutMilliseconds);
}

public sealed class WindowsSerialPrinterPorts:ISerialPrinterPorts
{
    public IReadOnlyList<string> Names()=>OperatingSystem.IsWindows()?SerialPort.GetPortNames():[];
    public ISerialPrinterConnection Open(string port)
    {
        if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException();
        // Windows supplies the outgoing SPP port after pairing. Enumeration never opens devices.
        // https://learn.microsoft.com/en-us/windows-hardware/drivers/print/bluetooth-printing
        var serial=new SerialPort(port,9600,Parity.None,8,StopBits.One)
        {Handshake=Handshake.None,WriteTimeout=5000,ReadTimeout=1000,DtrEnable=false,RtsEnable=false};
        try{serial.Open();return new Connection(serial);}
        catch{serial.Dispose();throw;}
    }
    private sealed class Connection(SerialPort serial):ISerialPrinterConnection
    {
        public int BytesToWrite=>serial.BytesToWrite;
        public void Write(byte[] data,int offset,int count,int timeoutMilliseconds)
        {serial.WriteTimeout=Math.Min(5000,timeoutMilliseconds);serial.Write(data,offset,count);}
        public void Dispose()=>serial.Dispose();
    }
}

public sealed class BluetoothPrintTransport(ISerialPrinterPorts ports,TimeSpan? connectionTimeout=null):IPrintTransport
{
    public IReadOnlyList<string> Printers()
    {
        try{return ports.Names().Where(port=>BluetoothPrinterTarget.TryPort("MP58-16 Bluetooth ("+port+")",out _))
            .Select(BluetoothPrinterTarget.ForPort).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();}
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {return [];}
    }
    public Task<PrintAcceptance> SendAsync(PrintDocument document)=>Task.Run(()=>SendCoreAsync(document));
    private async Task<PrintAcceptance> SendCoreAsync(PrintDocument document)
    {
        if(!BluetoothPrinterTarget.TryPort(document.Printer,out var port)||!Printers().Contains(document.Printer))
            throw new PrintDispatchException("El puerto Bluetooth no está disponible. Empareja la impresora y revisa su puerto COM saliente.",false);
        var bytes=ThermalReceipt.Encode(document.Text);
        var started=false;var clock=Stopwatch.StartNew();
        try
        {
            using var connection=await OpenAsync(port);
            for(var offset=0;offset<bytes.Length;offset+=256)
            {
                var remaining=30000-(int)clock.ElapsedMilliseconds;
                if(remaining<=0)throw new TimeoutException();
                // A write may transmit a partial chunk before throwing: never retry it automatically.
                started=true;connection.Write(bytes,offset,Math.Min(256,bytes.Length-offset),remaining);
                await Task.Delay(15);
            }
            while(connection.BytesToWrite>0)
            {
                if(clock.ElapsedMilliseconds>=30000)throw new TimeoutException();
                await Task.Delay(25);
            }
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or TimeoutException or InvalidOperationException or System.Security.SecurityException or PlatformNotSupportedException)
        {
            throw new PrintDispatchException(started
                ?"Se interrumpió el envío Bluetooth. Comprueba el papel antes de resolver; puede haber una impresión parcial."
                :"No se pudo abrir el puerto Bluetooth. Revisa conexión, batería y que otra aplicación no lo esté usando.",started);
        }
        return new("BT-"+document.RequestId+"-"+Guid.NewGuid().ToString("N"),
            $"Envío ESC/POS PC850 de {bytes.Length} bytes al puerto {port}; Windows vació su búfer. No confirma impresión ni entrega física.");
    }
    private async Task<ISerialPrinterConnection> OpenAsync(string port)
    {
        var pending=Task.Run(()=>ports.Open(port));
        try{return await pending.WaitAsync(connectionTimeout??TimeSpan.FromSeconds(10));}
        catch(TimeoutException)
        {
            // SerialPort.Open has no timeout setting. A late open is disposed without ever writing.
            _=pending.ContinueWith(result=>
            {
                if(result.IsCompletedSuccessfully){try{result.Result.Dispose();}catch{/* No bytes were sent. */}}
                else _=result.Exception;
            },CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
            throw;
        }
    }
}

public sealed class LocalPrintTransport(IPrintTransport windows,IPrintTransport bluetooth):IPrintTransport
{
    public IReadOnlyList<string> Printers()=>windows.Printers().Concat(bluetooth.Printers()).Distinct(StringComparer.Ordinal).ToList();
    public Task<PrintAcceptance> SendAsync(PrintDocument document)=>
        (BluetoothPrinterTarget.IsBluetooth(document.Printer)?bluetooth:windows).SendAsync(document);
}
