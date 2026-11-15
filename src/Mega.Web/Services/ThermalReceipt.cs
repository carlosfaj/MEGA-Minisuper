using System.Globalization;
using System.Text;

namespace Mega.Web.Services;

public static class BluetoothPrinterTarget
{
    private const string Prefix="MP58-16 Bluetooth (";
    public static string ForPort(string port)
    {
        if(!ValidPort(port))throw new ArgumentException("Puerto COM inválido.",nameof(port));
        return Prefix+port+")";
    }
    public static bool IsBluetooth(string printer)=>printer.StartsWith(Prefix,StringComparison.Ordinal);
    public static bool TryPort(string printer,out string port)
    {
        port="";
        if(!IsBluetooth(printer)||!printer.EndsWith(')'))return false;
        var candidate=printer[Prefix.Length..^1];
        if(!ValidPort(candidate))return false;
        port=candidate;return true;
    }
    private static bool ValidPort(string port)=>port.StartsWith("COM",StringComparison.Ordinal)
        &&int.TryParse(port.AsSpan(3),NumberStyles.None,CultureInfo.InvariantCulture,out var number)
        &&number is >0 and <=65535&&port=="COM"+number.ToString(CultureInfo.InvariantCulture);
}

public static class ThermalReceipt
{
    private static readonly Encoding CodePage=CreateEncoding();
    private static Encoding CreateEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(850,EncoderFallback.ExceptionFallback,DecoderFallback.ExceptionFallback);
    }

    public static byte[] Encode(string text)
    {
        // Epson ESC/POS: initialize, Font A, international USA and PC850 (ESC t 2).
        // https://download4.epson.biz/sec_pubs/pos/reference_en/escpos/esc_lt.html
        // No cut or cash-drawer commands: the portable printer is torn manually.
        var normalized=text.Replace("\r\n","\n").Replace('\r','\n');
        var safe=new string(normalized.Select(c=>c=='\n'?c:char.IsControl(c)?' ':c).ToArray());
        if(safe.Split('\n').Any(line=>line.Length>32))throw new PrintDispatchException("El ticket Bluetooth supera las 32 columnas.",false);
        byte[] body;
        try{body=CodePage.GetBytes(safe+"\n\n\n");}
        catch(EncoderFallbackException){throw new PrintDispatchException("El ticket contiene caracteres no admitidos por PC850. Revisa los textos o utiliza el controlador de Windows.",false);}
        if(body.Length>131072)throw new PrintDispatchException("El ticket Bluetooth supera el tamaño permitido.",false);
        return [0x1B,0x40,0x1B,0x4D,0,0x1B,0x52,0,0x1B,0x74,2,..body];
    }
}
