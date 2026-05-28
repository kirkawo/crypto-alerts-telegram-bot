using CryptoAlerts.Application.Dtos;
using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Application.Services;
using CryptoAlerts.Domain.Entities;
using CryptoAlerts.Domain.Enums;
using Moq;

namespace CryptoAlerts.UnitTests;

public class AlertProcessingServiceTests
{
    private readonly Mock<IAlertRepository> _alertRepo;
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<IPriceProvider> _priceProvider;
    private readonly Mock<ITelegramMessageSender> _messageSender;

    public AlertProcessingServiceTests()
    {
        _alertRepo = new Mock<IAlertRepository>();
        _userRepo = new Mock<IUserRepository>();
        _priceProvider = new Mock<IPriceProvider>();
        _messageSender = new Mock<ITelegramMessageSender>();
    }

    [Fact]
    public async Task DoesNotTrigger_WhenPriceBelowTarget()
    {
        var user = new TrackedUser(100, 200, "testuser");
        var alert = new PriceAlert(user.Id, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);

        _alertRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert]);

        _priceProvider.Setup(p => p.GetCurrentPriceAsync("bitcoin", "usd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult { Value = 40000m });

        var service = new AlertProcessingService(
            _alertRepo.Object, _userRepo.Object, _priceProvider.Object, _messageSender.Object);

        var triggered = await service.ProcessAlertsAsync();

        Assert.Equal(0, triggered);
        Assert.Equal(AlertStatus.Active, alert.Status);
        _messageSender.Verify(s => s.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TriggersAlert_AndSendsMessage_WhenPriceMatches()
    {
        var user = new TrackedUser(100, 200, "testuser");
        var alert = new PriceAlert(user.Id, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);

        _alertRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert]);

        _priceProvider.Setup(p => p.GetCurrentPriceAsync("bitcoin", "usd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult { Value = 55000m });

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new AlertProcessingService(
            _alertRepo.Object, _userRepo.Object, _priceProvider.Object, _messageSender.Object);

        var triggered = await service.ProcessAlertsAsync();

        Assert.Equal(1, triggered);
        Assert.Equal(AlertStatus.Triggered, alert.Status);
        _messageSender.Verify(s => s.SendMessageAsync(user.TelegramChatId, It.Is<string>(m => m.Contains("BTC")), It.IsAny<CancellationToken>()));
        _alertRepo.Verify(r => r.UpdateAsync(It.Is<PriceAlert>(a => a.Status == AlertStatus.Triggered), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task SkipsAlert_WhenUserNotFound()
    {
        var alert = new PriceAlert(Guid.NewGuid(), "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);

        _alertRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert]);

        _priceProvider.Setup(p => p.GetCurrentPriceAsync("bitcoin", "usd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult { Value = 55000m });

        _userRepo.Setup(r => r.GetByIdAsync(alert.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackedUser?)null);

        var service = new AlertProcessingService(
            _alertRepo.Object, _userRepo.Object, _priceProvider.Object, _messageSender.Object);

        var triggered = await service.ProcessAlertsAsync();

        Assert.Equal(0, triggered);
        _messageSender.Verify(s => s.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ContinuesProcessing_WhenSingleAlertFails()
    {
        var user = new TrackedUser(100, 200, "testuser");
        var goodAlert = new PriceAlert(user.Id, "ETH", "ethereum", 3000m, AlertCondition.GreaterOrEqual);
        var badAlert = new PriceAlert(user.Id, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);

        _alertRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([badAlert, goodAlert]);

        _priceProvider.Setup(p => p.GetCurrentPriceAsync("bitcoin", "usd", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API down"));

        _priceProvider.Setup(p => p.GetCurrentPriceAsync("ethereum", "usd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult { Value = 3500m });

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new AlertProcessingService(
            _alertRepo.Object, _userRepo.Object, _priceProvider.Object, _messageSender.Object);

        var triggered = await service.ProcessAlertsAsync();

        Assert.Equal(1, triggered);
        _messageSender.Verify(s => s.SendMessageAsync(It.IsAny<long>(), It.Is<string>(m => m.Contains("ETH")), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task ProcessesMultipleAlerts_CorrectCount()
    {
        var user = new TrackedUser(100, 200, "testuser");
        var alert1 = new PriceAlert(user.Id, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);
        var alert2 = new PriceAlert(user.Id, "ETH", "ethereum", 3000m, AlertCondition.GreaterOrEqual);
        var alert3 = new PriceAlert(user.Id, "SOL", "solana", 150m, AlertCondition.GreaterOrEqual);

        _alertRepo.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([alert1, alert2, alert3]);

        _priceProvider.Setup(p => p.GetCurrentPriceAsync("bitcoin", "usd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult { Value = 40000m });

        _priceProvider.Setup(p => p.GetCurrentPriceAsync("ethereum", "usd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult { Value = 3500m });

        _priceProvider.Setup(p => p.GetCurrentPriceAsync("solana", "usd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult { Value = 200m });

        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = new AlertProcessingService(
            _alertRepo.Object, _userRepo.Object, _priceProvider.Object, _messageSender.Object);

        var triggered = await service.ProcessAlertsAsync();

        Assert.Equal(2, triggered);
        _messageSender.Verify(s => s.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
