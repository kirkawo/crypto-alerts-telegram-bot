using CryptoAlerts.Domain.Enums;

namespace CryptoAlerts.Domain.Entities;

public class PriceAlert
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string AssetSymbol { get; private set; } = null!;
    public string AssetId { get; private set; } = null!;
    public decimal TargetPrice { get; private set; }
    public AlertCondition Condition { get; private set; }
    public AlertStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? TriggeredAtUtc { get; private set; }

    private PriceAlert() { }

    public PriceAlert(Guid userId, string assetSymbol, string assetId, decimal targetPrice, AlertCondition condition)
    {
        if (string.IsNullOrWhiteSpace(assetSymbol))
            throw new ArgumentException("AssetSymbol is required.", nameof(assetSymbol));

        if (string.IsNullOrWhiteSpace(assetId))
            throw new ArgumentException("AssetId is required.", nameof(assetId));

        if (targetPrice <= 0)
            throw new ArgumentException("TargetPrice must be greater than zero.", nameof(targetPrice));

        Id = Guid.NewGuid();
        UserId = userId;
        AssetSymbol = assetSymbol;
        AssetId = assetId;
        TargetPrice = targetPrice;
        Condition = condition;
        Status = AlertStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Trigger()
    {
        if (Status == AlertStatus.Triggered)
            throw new InvalidOperationException("A triggered alert cannot be triggered again.");

        if (Status == AlertStatus.Cancelled)
            throw new InvalidOperationException("A cancelled alert cannot be triggered.");

        Status = AlertStatus.Triggered;
        TriggeredAtUtc = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != AlertStatus.Active)
            throw new InvalidOperationException("Only active alerts can be cancelled.");

        Status = AlertStatus.Cancelled;
    }
}
