using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace Mega.Web.Services;
public static class Formats
{
    public static string Now() => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    public static string Money(long cents) => "C$ " + (cents / 100m).ToString("N2", CultureInfo.GetCultureInfo("es-NI"));
    public static string Date(string utc) => DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture).ToOffset(TimeSpan.FromHours(-6)).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
    public static string DateWithSeconds(string utc) => DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture).ToOffset(TimeSpan.FromHours(-6)).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    public static string Json<T>(T value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    public static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static long Scaled(decimal value, int scale)
    {
        try
        {
            var factor = (decimal)Math.Pow(10, scale);
            var scaled = checked(value * factor);
            if (value < 0 || scaled != decimal.Truncate(scaled)) throw new BusinessException($"El valor admite hasta {scale} decimales.");
            return checked((long)scaled);
        }
        catch (OverflowException) { throw new BusinessException("El valor supera la capacidad de registro."); }
    }
    public static decimal Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 32 || !decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            throw new BusinessException("Introduce un número válido, con punto para los decimales.");
        return value;
    }
}
public sealed class BusinessException:Exception
{
    public BusinessException(string message):base(message)=>RejectionCapture.Record(message);
}
public sealed record LineInput(long PresentationId, string Mode, string Value);
public sealed record SaleInput(string Key, string QuoteHash, string Received, bool PhysicalStockChecked, List<LineInput> Lines);
public sealed record ProductOption(long Id, string Name, string Presentation, string Code, string Unit, string Control, long QuantityScale, long Price, decimal Stock);
public sealed record QuoteLine(long PresentationId, long ProductId, string Name, string Presentation, string Unit, string Mode,
    long Quantity, long QuantityScale, long BaseQuantity, long Factor, long FactorScale, long Requested, long RequestedScale,
    long Price, long Subtotal, long ProductVersion, long PresentationVersion, long PriceId, decimal Stock)
{
    public string CostState { get; init; } = "desconocido";
    public long? BaseCost { get; init; }
    public long? CostReferenceId { get; init; }
    public string ValuationHash { get; init; } = "";
    public List<StockAllocation> Allocations { get; init; } = [];
    public long Discount { get; init; }
    public long Tax { get; init; }
    public long Total { get; init; }
    public AppliedPromotion? Promotion { get; init; }
    public List<AppliedTax> Taxes { get; init; } = [];
}
public sealed record Quote(List<QuoteLine> Lines, long Total, string Hash, long ConfigurationId, bool NeedsPhysicalCheck)
{
    public long Subtotal=>Lines.Sum(x=>x.Subtotal);
    public long Tax=>Lines.Sum(x=>x.Tax);
    public long Discount=>Lines.Sum(x=>x.Discount);
}
public sealed record CashSummary(Mega.Web.Data.TurnoCaja? Shift, long Net, long Expected, long Usual, bool StationOccupied);
