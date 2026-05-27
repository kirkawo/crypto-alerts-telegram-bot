using CryptoAlerts.Application.Dtos;
using CryptoAlerts.Application.Exceptions;
using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Application.Services;
using CryptoAlerts.Domain.Entities;
using CryptoAlerts.Domain.Enums;
using Moq;

namespace CryptoAlerts.UnitTests;

public class ApplicationServiceTests
{
    private readonly Mock<ISymbolResolver> _symbolResolver;
    private readonly Mock<IPriceProvider> _priceProvider;
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IAlertRepository> _alertRepository;

    public ApplicationServiceTests()
    {
        _symbolResolver = new Mock<ISymbolResolver>();
        _priceProvider = new Mock<IPriceProvider>();
        _userRepository = new Mock<IUserRepository>();
        _alertRepository = new Mock<IAlertRepository>();
    }

    [Fact]
    public async Task PriceQueryService_ReturnsPrice_ForKnownSymbol()
    {
        _symbolResolver.Setup(r => r.ResolveAssetIdAsync("BTC", It.IsAny<CancellationToken>()))
            .ReturnsAsync("bitcoin");

        _priceProvider.Setup(p => p.GetCurrentPriceAsync("bitcoin", "USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult
            {
                AssetSymbol = "BTC",
                AssetId = "bitcoin",
                Currency = "USD",
                Value = 50000m,
                RetrievedAtUtc = DateTime.UtcNow
            });

        var service = new PriceQueryService(_symbolResolver.Object, _priceProvider.Object);

        var result = await service.GetPriceAsync("BTC", "USD");

        Assert.Equal("BTC", result.AssetSymbol);
        Assert.Equal("bitcoin", result.AssetId);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(50000m, result.Value);
    }

    [Fact]
    public async Task PriceQueryService_Throws_ForUnknownSymbol()
    {
        _symbolResolver.Setup(r => r.ResolveAssetIdAsync("UNKNOWN", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnknownSymbolException("UNKNOWN"));

        var service = new PriceQueryService(_symbolResolver.Object, _priceProvider.Object);

        await Assert.ThrowsAsync<UnknownSymbolException>(() =>
            service.GetPriceAsync("UNKNOWN", "USD"));
    }

    [Fact]
    public async Task AlertCommandService_CreatesUser_WhenMissing()
    {
        var request = new CreateAlertRequest
        {
            TelegramChatId = 100,
            TelegramUserId = 200,
            Username = "testuser",
            AssetSymbol = "BTC",
            TargetPrice = 50000m
        };

        _userRepository.Setup(r => r.GetByTelegramAsync(100, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackedUser?)null);

        _symbolResolver.Setup(r => r.ResolveAssetIdAsync("BTC", It.IsAny<CancellationToken>()))
            .ReturnsAsync("bitcoin");

        var service = new AlertCommandService(
            _userRepository.Object, _alertRepository.Object, _symbolResolver.Object);

        var result = await service.CreateAlertAsync(request);

        _userRepository.Verify(r => r.AddAsync(
            It.Is<TrackedUser>(u => u.TelegramChatId == 100 && u.TelegramUserId == 200), It.IsAny<CancellationToken>()));

        _alertRepository.Verify(r => r.AddAsync(
            It.IsAny<PriceAlert>(), It.IsAny<CancellationToken>()));

        Assert.Equal("BTC", result.AssetSymbol);
        Assert.Equal(50000m, result.TargetPrice);
        Assert.Equal(AlertStatus.Active, result.Status);
    }

    [Fact]
    public async Task AlertCommandService_CreatesAlert_ForExistingUser()
    {
        var existingUser = new TrackedUser(100, 200, "testuser");

        var request = new CreateAlertRequest
        {
            TelegramChatId = 100,
            TelegramUserId = 200,
            Username = "testuser",
            AssetSymbol = "BTC",
            TargetPrice = 50000m
        };

        _userRepository.Setup(r => r.GetByTelegramAsync(100, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _symbolResolver.Setup(r => r.ResolveAssetIdAsync("BTC", It.IsAny<CancellationToken>()))
            .ReturnsAsync("bitcoin");

        var service = new AlertCommandService(
            _userRepository.Object, _alertRepository.Object, _symbolResolver.Object);

        var result = await service.CreateAlertAsync(request);

        // Verify user was NOT added (already existed)
        _userRepository.Verify(r => r.AddAsync(
            It.IsAny<TrackedUser>(), It.IsAny<CancellationToken>()), Times.Never);

        _alertRepository.Verify(r => r.AddAsync(
            It.IsAny<PriceAlert>(), It.IsAny<CancellationToken>()));

        Assert.Equal("BTC", result.AssetSymbol);
        Assert.Equal(50000m, result.TargetPrice);
        Assert.Equal(AlertStatus.Active, result.Status);
    }

    [Fact]
    public async Task AlertCommandService_GetUserAlerts_ReturnsEmpty_ForUnknownUser()
    {
        _userRepository.Setup(r => r.GetByTelegramAsync(999, 999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackedUser?)null);

        var service = new AlertCommandService(
            _userRepository.Object, _alertRepository.Object, _symbolResolver.Object);

        var results = await service.GetUserAlertsAsync(999, 999);

        Assert.Empty(results);
    }

    [Fact]
    public async Task AlertCommandService_CancelsOwnAlert()
    {
        var user = new TrackedUser(100, 200, "testuser");
        var alert = new PriceAlert(user.Id, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);

        _userRepository.Setup(r => r.GetByTelegramAsync(100, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _alertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var service = new AlertCommandService(
            _userRepository.Object, _alertRepository.Object, _symbolResolver.Object);

        await service.CancelAlertAsync(alert.Id, 100, 200);

        Assert.Equal(AlertStatus.Cancelled, alert.Status);
    }

    [Fact]
    public async Task AlertCommandService_RejectsCancellingAnotherUsersAlert()
    {
        var user = new TrackedUser(100, 200, "testuser");
        var otherUser = new TrackedUser(300, 400, "other");
        var alert = new PriceAlert(otherUser.Id, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);

        _userRepository.Setup(r => r.GetByTelegramAsync(100, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _alertRepository.Setup(r => r.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        var service = new AlertCommandService(
            _userRepository.Object, _alertRepository.Object, _symbolResolver.Object);

        await Assert.ThrowsAsync<AlertAccessDeniedException>(() =>
            service.CancelAlertAsync(alert.Id, 100, 200));
    }
}
