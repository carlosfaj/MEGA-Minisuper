using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Mega.Web.Services;

public sealed record PrintDocument(long RequestId,string Printer,string Folio,long CopyNumber,string Text);
public sealed record PrintAcceptance(string JobId,string Evidence);
public sealed class PrintDispatchException(string code,bool uncertain):Exception(code)
{public string Code{get;}=code;public bool Uncertain{get;}=uncertain;}
public interface IPrintTransport
{
    IReadOnlyList<string> Printers();
    Task<PrintAcceptance> SendAsync(PrintDocument document);
}

public static class ReceiptText
{
    public static string Build(string snapshot,long copy)
    {
        using var doc=JsonDocument.Parse(snapshot);var r=doc.RootElement;
        var rows=new List<string>{r.GetProperty("negocio").GetString()??"MEGA",r.GetProperty("direccion").GetString()??"",
            "COMPROBANTE INTERNO", "Sin validez fiscal acreditada",r.GetProperty("folio").GetString()??"",copy==0?"ORIGINAL":$"COPIA #{copy}",
            Formats.Date(r.GetProperty("fecha").GetString()!),"Operador: "+r.GetProperty("operador").GetString(),new('-',40)};
        foreach(var line in r.GetProperty("lineas").EnumerateArray())
        {
            rows.Add(line.GetProperty("name").GetString()??"");
            var quantity=line.GetProperty("quantity").GetInt64()/(decimal)Math.Pow(10,line.GetProperty("quantityScale").GetInt32());
            rows.Add($"{quantity.ToString("0.###",CultureInfo.InvariantCulture)} {line.GetProperty("presentation").GetString()} x {Formats.Money(line.GetProperty("price").GetInt64())}");
            rows.Add("Importe: "+Formats.Money(line.TryGetProperty("total",out var total)?total.GetInt64():line.GetProperty("subtotal").GetInt64()));
        }
        rows.Add(new('-',40));
        foreach(var (name,key) in new[]{("Subtotal","subtotalCentavos"),("Descuento","descuentoCentavos"),("Impuesto","impuestoCentavos"),("TOTAL","totalCentavos")})
            rows.Add(name+": "+Formats.Money(r.GetProperty(key).GetInt64()));
        foreach(var payment in r.GetProperty("pagos").EnumerateArray())
        {rows.Add("Recibido: "+Formats.Money(payment.GetProperty("recibidoCentavos").GetInt64()));rows.Add("Cambio: "+Formats.Money(payment.GetProperty("cambioCentavos").GetInt64()));}
        return string.Join('\n',rows.SelectMany(Wrap));
    }
    private static IEnumerable<string> Wrap(string value)
    {
        var text=new string(value.Select(c=>char.IsControl(c)?' ':c).ToArray());
        if(text.Length==0){yield return "";yield break;}
        while(text.Length>40)
        {
            var cut=text.LastIndexOf(' ',40);if(cut<=0)cut=40;
            yield return text[..cut];text=text[cut..].TrimStart();
        }
        if(text.Length>0)yield return text;
    }
}

public sealed class WindowsPrintTransport(OperationalFiles files):IPrintTransport
{
    public IReadOnlyList<string> Printers()
    {
        if(!OperatingSystem.IsWindows())return [];
        Native.EnumPrinters(6,null,4,IntPtr.Zero,0,out var bytes,out _);if(bytes==0)return [];
        var buffer=Marshal.AllocHGlobal(checked((int)bytes));
        try
        {
            if(!Native.EnumPrinters(6,null,4,buffer,bytes,out _,out var count))throw new PrintDispatchException("No se pudieron consultar las impresoras de Windows.",false);
            var size=Marshal.SizeOf<Native.PrinterInfo>();var result=new List<string>();
            for(var i=0;i<count;i++){var p=Marshal.PtrToStructure<Native.PrinterInfo>(IntPtr.Add(buffer,checked(i*size)));var name=Marshal.PtrToStringUni(p.Name);if(!string.IsNullOrWhiteSpace(name))result.Add(name);}
            return result.Distinct(StringComparer.Ordinal).Order().ToList();
        }
        finally{Marshal.FreeHGlobal(buffer);}
    }
    public Task<PrintAcceptance> SendAsync(PrintDocument document)=>Task.Run(()=>Send(document));
    private PrintAcceptance Send(PrintDocument document)
    {
        if(!OperatingSystem.IsWindows()||!Printers().Contains(document.Printer))throw new PrintDispatchException("Impresora no instalada en este equipo.",false);
        if(document.Printer.Contains("OneNote",StringComparison.OrdinalIgnoreCase))throw new PrintDispatchException("Esta impresora requiere interacción y no admite el agente automático.",false);
        string? output=null;
        if(document.Printer.Equals("Microsoft Print to PDF",StringComparison.OrdinalIgnoreCase))
        {var dir=Path.Combine(files.Root,"Impresiones");Directory.CreateDirectory(dir);output=Path.Combine(dir,$"comprobante-{document.RequestId}-{Guid.NewGuid():N}.pdf");}
        var dc=Native.CreateDC(null,document.Printer,null,IntPtr.Zero);if(dc==IntPtr.Zero)throw new PrintDispatchException("Windows no pudo abrir la impresora.",false);
        var started=false;IntPtr font=IntPtr.Zero,previousFont=IntPtr.Zero;
        try
        {
            var info=new Native.DocInfo{Size=Marshal.SizeOf<Native.DocInfo>(),Name="MEGA "+document.Folio+(document.CopyNumber>0?$" COPIA {document.CopyNumber}":" ORIGINAL"),Output=output};
            var job=Native.StartDoc(dc,ref info);if(job<=0)throw new PrintDispatchException("Windows no aceptó el trabajo de impresión.",false);started=true;
            var dpi=Native.GetDeviceCaps(dc,90);var step=Math.Max(12,dpi/6);var margin=Math.Max(8,dpi/10);var height=Native.GetDeviceCaps(dc,10);
            var fontHeight=Math.Max(8,Math.Min(dpi/8,(Native.GetDeviceCaps(dc,8)-2*margin)/25));
            font=Native.CreateFont(-fontHeight,0,0,0,400,0,0,0,1,0,0,0,1,"Courier New");
            if(font==IntPtr.Zero)throw new PrintDispatchException("No se pudo preparar el texto de impresión.",true);
            previousFont=Native.SelectObject(dc,font);var y=margin;
            if(Native.StartPage(dc)<=0)throw new PrintDispatchException("No se pudo iniciar la página.",true);
            foreach(var line in document.Text.Split('\n'))
            {
                if(y+step>height-margin){if(Native.EndPage(dc)<=0||Native.StartPage(dc)<=0)throw new PrintDispatchException("Impresión interrumpida entre páginas.",true);y=margin;}
                if(!Native.TextOut(dc,margin,y,line,line.Length))throw new PrintDispatchException("Impresión interrumpida al dibujar el texto.",true);y+=step;
            }
            if(Native.EndPage(dc)<=0||Native.EndDoc(dc)<=0)throw new PrintDispatchException("No se pudo confirmar el final del trabajo.",true);
            started=false;return new(job.ToString(CultureInfo.InvariantCulture),output is null?"Windows aceptó el trabajo; la entrega física requiere comprobación.":"Salida virtual PDF: "+output);
        }
        catch(PrintDispatchException){throw;}
        catch{throw new PrintDispatchException("Error del controlador de impresión.",started);}
        finally{if(started)Native.AbortDoc(dc);if(previousFont!=IntPtr.Zero)Native.SelectObject(dc,previousFont);if(font!=IntPtr.Zero)Native.DeleteObject(font);Native.DeleteDC(dc);}
    }
    private static class Native
    {
        [StructLayout(LayoutKind.Sequential)]internal struct PrinterInfo{internal IntPtr Name;internal IntPtr Server;internal uint Attributes;}
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]internal struct DocInfo{internal int Size;[MarshalAs(UnmanagedType.LPWStr)]internal string Name;[MarshalAs(UnmanagedType.LPWStr)]internal string? Output;[MarshalAs(UnmanagedType.LPWStr)]internal string? DataType;internal uint Flags;}
        [DllImport("winspool.drv",EntryPoint="EnumPrintersW",CharSet=CharSet.Unicode,SetLastError=true)][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool EnumPrinters(uint flags,string? name,uint level,IntPtr buffer,uint bytes,out uint needed,out uint returned);
        [DllImport("gdi32.dll",EntryPoint="CreateDCW",CharSet=CharSet.Unicode)]internal static extern IntPtr CreateDC(string? driver,string device,string? port,IntPtr mode);
        [DllImport("gdi32.dll",EntryPoint="StartDocW",CharSet=CharSet.Unicode)]internal static extern int StartDoc(IntPtr dc,ref DocInfo info);
        [DllImport("gdi32.dll")]internal static extern int StartPage(IntPtr dc);
        [DllImport("gdi32.dll")]internal static extern int EndPage(IntPtr dc);
        [DllImport("gdi32.dll")]internal static extern int EndDoc(IntPtr dc);
        [DllImport("gdi32.dll")]internal static extern int AbortDoc(IntPtr dc);
        [DllImport("gdi32.dll")]internal static extern int GetDeviceCaps(IntPtr dc,int index);
        [DllImport("gdi32.dll",EntryPoint="TextOutW",CharSet=CharSet.Unicode)][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool TextOut(IntPtr dc,int x,int y,string text,int count);
        [DllImport("gdi32.dll",EntryPoint="CreateFontW",CharSet=CharSet.Unicode)]internal static extern IntPtr CreateFont(int height,int width,int escapement,int orientation,int weight,uint italic,uint underline,uint strikeout,uint charset,uint output,uint clip,uint quality,uint pitch,string face);
        [DllImport("gdi32.dll")]internal static extern IntPtr SelectObject(IntPtr dc,IntPtr item);
        [DllImport("gdi32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool DeleteObject(IntPtr item);
        [DllImport("gdi32.dll")][return:MarshalAs(UnmanagedType.Bool)]internal static extern bool DeleteDC(IntPtr dc);
    }
}
