using CryptoAlerts.Domain.Entities;
using CryptoAlerts.Domain.Enums;

namespace CryptoAlerts.UnitTests;

public class PriceAlertTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_SetsPropertiesCorrectly()
    {
        var alert = new PriceAlert(_userId, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);

        Assert.Equal(_userId, alert.UserId);
        Assert.Equal("BTC", alert.AssetSymbol);
        Assert.Equal("bitcoin", alert.AssetId);
        Assert.Equal(50000m, alert.TargetPrice);
        Assert.Equal(AlertCondition.GreaterOrEqual, alert.Condition);
        Assert.Equal(AlertStatus.Active, alert.Status);
        Assert.NotEqual(Guid.Empty, alert.Id);
        Assert.True(alert.CreatedAtUtc > DateTime.UtcNow.AddMinutes(-1));
        Assert.Null(alert.TriggeredAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Create_WithInvalidTargetPrice_Throws(decimal targetPrice)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new PriceAlert(_userId, "BTC", "bitcoin", targetPrice, AlertCondition.GreaterOrEqual));

        Assert.Contains("TargetPrice", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithMissingAssetSymbol_Throws(string? assetSymbol)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new PriceAlert(_userId, assetSymbol!, "bitcoin", 50000m, AlertCondition.GreaterOrEqual));

        Assert.Contains("AssetSymbol", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithMissingAssetId_Throws(string? assetId)
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new PriceAlert(_userId, "BTC", assetId!, 50000m, AlertCondition.GreaterOrEqual));

        Assert.Contains("AssetId", ex.Message);
    }

    [Fact]
    public void Trigger_ActiveAlert_SetsTriggered()
    {
        var alert = new PriceAlert(_userId, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);

        alert.Trigger();

        Assert.Equal(AlertStatus.Triggered, alert.Status);
        Assert.NotNull(alert.TriggeredAtUtc);
        Assert.True(alert.TriggeredAtUtc > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Trigger_AlreadyTriggeredAlert_Throws()
    {
        var alert = new PriceAlert(_userId, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);
        alert.Trigger();

        var ex = Assert.Throws<InvalidOperationException>(() => alert.Trigger());

        Assert.Contains("triggered again", ex.Message);
    }

    [Fact]
    public void Trigger_CancelledAlert_Throws()
    {
        var alert = new PriceAlert(_userId, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);
        alert.Cancel();

        var ex = Assert.Throws<InvalidOperationException>(() => alert.Trigger());

        Assert.Contains("cancelled", ex.Message);
    }

    [Fact]
    public void Cancel_ActiveAlert_SetsCancelled()
    {
        var alert = new PriceAlert(_userId, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);

        alert.Cancel();

        Assert.Equal(AlertStatus.Cancelled, alert.Status);
    }

    [Fact]
    public void Cancel_TriggeredAlert_Throws()
    {
        var alert = new PriceAlert(_userId, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);
        alert.Trigger();

        var ex = Assert.Throws<InvalidOperationException>(() => alert.Cancel());

        Assert.Contains("active", ex.Message);
    }
}
