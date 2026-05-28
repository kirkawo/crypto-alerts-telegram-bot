using CryptoAlerts.Domain.Enums;

namespace CryptoAlerts.Application.Dtos;

public class AlertListItem
{
    public Guid AlertId { get; init; }
    public string AssetSymbol { get; init; } = null!;
    public decimal TargetPrice { get; init; }
    public AlertStatus Status { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
