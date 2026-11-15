using System.Text;
using System.Text.Json;
using Mega.Web.Services;

namespace Mega.Tests;

public sealed class ThermalReceiptTests
{
    internal static string Snapshot=>JsonSerializer.Serialize(new{
        negocio="Minisúper MEGA",direccion=new string('L',75),folio="MEGA-00000001",fecha="2026-11-15T12:00:00.000Z",operador="María Núñez",
        lineas=new[]{new{name="Café molido presentación familiar de prueba",quantity=1250,quantityScale=3,presentation="lb",price=4000,total=5000}},
        subtotalCentavos=5000,descuentoCentavos=0,impuestoCentavos=0,totalCentavos=5000,
        pagos=new[]{new{recibidoCentavos=10000,cambioCentavos=5000}}});

    [Fact]
    public void narrow_ticket_keeps_folio_amounts_spanish_and_copy_without_clipping()
    {
        var text=ReceiptText.Build(Snapshot,2,32);
        Assert.All(text.Split('\n'),line=>Assert.True(line.Length<=32));
        foreach(var expected in new[]{"MEGA-00000001","COPIA #2","María Núñez","1.25 lb x C$ 40.00","TOTAL: C$ 50.00","Cambio: C$ 50.00"})Assert.Contains(expected,text);
        Assert.Contains(new string('-',32),text);
        Assert.Equal(75,string.Concat(text.Split('\n').Where(line=>line.All(c=>c=='L'))).Length);
    }
    [Fact]
    public void existing_windows_layout_keeps_40_columns_by_default()
    {
        Assert.Equal(ReceiptText.Build(Snapshot,0,40),ReceiptText.Build(Snapshot,0));
        Assert.Contains(new string('-',40),ReceiptText.Build(Snapshot,0));
        Assert.Throws<ArgumentOutOfRangeException>(()=>ReceiptText.Build(Snapshot,0,0));
    }
    [Theory]
    [InlineData("COM1")][InlineData("COM15")][InlineData("COM65535")]
    public void only_canonical_com_names_can_be_used(string port)
    {Assert.True(BluetoothPrinterTarget.TryPort(BluetoothPrinterTarget.ForPort(port),out var actual));Assert.Equal(port,actual);}
    [Theory]
    [InlineData("COM0")][InlineData("COM01")][InlineData("COM65536")][InlineData("COM3\\file")][InlineData("COM3:")][InlineData("LPT1")][InlineData("com3")][InlineData("COM+3")]
    public void malformed_or_path_like_ports_are_rejected(string port)
    {Assert.Throws<ArgumentException>(()=>BluetoothPrinterTarget.ForPort(port));Assert.False(BluetoothPrinterTarget.TryPort("MP58-16 Bluetooth ("+port+")",out _));}
    [Fact]
    public void escpos_preserves_spanish_as_pc850_and_cannot_inject_device_commands()
    {
        var bytes=ThermalReceipt.Encode("Café, piñón, María\nTOTAL: C$50.00\n\u001b\u001dp");
        Assert.Equal(new byte[]{0x1B,0x40,0x1B,0x4D,0,0x1B,0x52,0,0x1B,0x74,2},bytes[..11]);
        var body=Encoding.GetEncoding(850).GetString(bytes[11..]);
        Assert.Contains("Café, piñón, María",body);Assert.EndsWith("  p\n\n\n",body);
        Assert.DoesNotContain((byte)0x1B,bytes[11..]);Assert.DoesNotContain((byte)0x1D,bytes[11..]);
    }
    [Theory]
    [InlineData("Carácter 漢 no representable")][InlineData("123456789012345678901234567890123")]
    public void unprintable_content_fails_before_sending_instead_of_corrupting_ticket(string text)
    {Assert.False(Assert.Throws<PrintDispatchException>(()=>ThermalReceipt.Encode(text)).Uncertain);}
}
