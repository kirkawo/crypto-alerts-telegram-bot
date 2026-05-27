namespace CryptoAlerts.Application.Dtos;

public class PriceResult
{
    public string AssetSymbol { get; init; } = null!;
    public string AssetId { get; init; } = null!;
    public string Currency { get; init; } = null!;
    public decimal Value { get; init; }
    public DateTime RetrievedAtUtc { get; init; }
}
