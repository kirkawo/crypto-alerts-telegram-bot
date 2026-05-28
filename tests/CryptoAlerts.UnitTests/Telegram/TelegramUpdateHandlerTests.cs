using System.Globalization;
using CryptoAlerts.Application.Dtos;
using CryptoAlerts.Application.Exceptions;
using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Application.Services;
using CryptoAlerts.Bot.Telegram;
using CryptoAlerts.Domain.Entities;
using CryptoAlerts.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CryptoAlerts.UnitTests.Telegram;

public class TelegramUpdateHandlerTests
{
    private readonly Mock<IPriceProvider> _priceProviderMock;
    private readonly Mock<ISymbolResolver> _symbolResolverMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAlertRepository> _alertRepositoryMock;
    private readonly Mock<ITelegramMessageSender> _messageSenderMock;
    private readonly TelegramUpdateHandler _handler;

    public TelegramUpdateHandlerTests()
    {
        _priceProviderMock = new Mock<IPriceProvider>();
        _symbolResolverMock = new Mock<ISymbolResolver>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _alertRepositoryMock = new Mock<IAlertRepository>();
        _messageSenderMock = new Mock<ITelegramMessageSender>();

        var priceQueryService = new PriceQueryService(
            _symbolResolverMock.Object, _priceProviderMock.Object);

        var alertCommandService = new AlertCommandService(
            _userRepositoryMock.Object, _alertRepositoryMock.Object, _symbolResolverMock.Object);

        var loggerMock = new Mock<ILogger<TelegramUpdateHandler>>();
        var optionsMock = new Mock<IOptions<TelegramBotOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new TelegramBotOptions { BotUsername = "" });

        _handler = new TelegramUpdateHandler(
            priceQueryService, alertCommandService, _messageSenderMock.Object,
            loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public async Task HandleAsync_StartMessage_SendsWelcome()
    {
        var update = CreateTextUpdate("/start");

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("Welcome")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_HelpMessage_SendsHelp()
    {
        var update = CreateTextUpdate("/help");

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("/price")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_PriceWithSymbol_SendsPrice()
    {
        var update = CreateTextUpdate("/price BTC");

        _symbolResolverMock
            .Setup(r => r.ResolveAssetIdAsync("BTC", It.IsAny<CancellationToken>()))
            .ReturnsAsync("bitcoin");

        _priceProviderMock
            .Setup(p => p.GetCurrentPriceAsync("bitcoin", "usd", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceResult
            {
                AssetSymbol = "BTC",
                AssetId = "bitcoin",
                Currency = "usd",
                Value = 45123.45m,
                RetrievedAtUtc = DateTime.UtcNow
            });

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("45123") && s.Contains("USD")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_PriceWithoutSymbol_SendsUsageHint()
    {
        var update = CreateTextUpdate("/price");

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("Usage")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_UnknownSymbol_SendsErrorMessage()
    {
        var update = CreateTextUpdate("/price UNKNOWN");

        _symbolResolverMock
            .Setup(r => r.ResolveAssetIdAsync("UNKNOWN", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnknownSymbolException("UNKNOWN"));

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("UNKNOWN") && s.Contains("symbol")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_UnknownCommand_SendsFallback()
    {
        var update = CreateTextUpdate("/xyz");

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("Unknown command")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_NonTextMessage_DoesNothing()
    {
        var update = new Update
        {
            Id = 1,
            Message = new Message
            {
                MessageId = 1,
                Chat = new Chat { Id = 42 },
                Date = DateTime.UtcNow
            }
        };

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(
            m => m.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NonMessageUpdate_DoesNothing()
    {
        var update = new Update
        {
            Id = 1,
            EditedMessage = new Message
            {
                MessageId = 1,
                Text = "/start",
                Chat = new Chat { Id = 42 },
                Date = DateTime.UtcNow
            }
        };

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(
            m => m.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SetAlert_WithValidArgs_CreatesAlert()
    {
        var update = CreateTextUpdate("/set_alert BTC 70000", fromUserId: 200);

        _symbolResolverMock
            .Setup(r => r.ResolveAssetIdAsync("BTC", It.IsAny<CancellationToken>()))
            .ReturnsAsync("bitcoin");

        _userRepositoryMock
            .Setup(r => r.GetByTelegramAsync(42, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackedUser?)null);

        await _handler.HandleAsync(update);

        _alertRepositoryMock.Verify(r => r.AddAsync(
            It.Is<PriceAlert>(a => a.AssetSymbol == "BTC" && a.TargetPrice == 70000m),
            It.IsAny<CancellationToken>()));

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("Alert set") && s.Contains("BTC") && s.Contains("70000")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_SetAlert_MissingArguments_SendsUsageHint()
    {
        var update = CreateTextUpdate("/set_alert", fromUserId: 200);

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("Usage")),
            It.IsAny<CancellationToken>()));

        _alertRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<PriceAlert>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_SetAlert_InvalidPrice_SendsErrorMessage()
    {
        var update = CreateTextUpdate("/set_alert BTC abc", fromUserId: 200);

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("Invalid price")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_SetAlert_UnknownSymbol_SendsErrorMessage()
    {
        var update = CreateTextUpdate("/set_alert UNKNOWN 100", fromUserId: 200);

        _symbolResolverMock
            .Setup(r => r.ResolveAssetIdAsync("UNKNOWN", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnknownSymbolException("UNKNOWN"));

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("UNKNOWN") && s.Contains("symbol")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_ListAlerts_Empty_SendsEmptyState()
    {
        var update = CreateTextUpdate("/list_alerts", fromUserId: 200);

        _userRepositoryMock
            .Setup(r => r.GetByTelegramAsync(42, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrackedUser?)null);

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("no alerts")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_ListAlerts_WithAlerts_SendsList()
    {
        var update = CreateTextUpdate("/list_alerts", fromUserId: 200);
        var user = new TrackedUser(42, 200, "testuser");

        _userRepositoryMock
            .Setup(r => r.GetByTelegramAsync(42, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _alertRepositoryMock
            .Setup(r => r.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceAlert>
            {
                new(user.Id, "BTC", "bitcoin", 70000m, AlertCondition.GreaterOrEqual)
            });

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("BTC") && s.Contains("70000")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_RemoveAlert_ValidId_SendsConfirmation()
    {
        var alertId = Guid.NewGuid();
        var update = CreateTextUpdate($"/remove_alert {alertId}", fromUserId: 200);
        var user = new TrackedUser(42, 200, "testuser");

        _userRepositoryMock
            .Setup(r => r.GetByTelegramAsync(42, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _alertRepositoryMock
            .Setup(r => r.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceAlert(user.Id, "BTC", "bitcoin", 70000m, AlertCondition.GreaterOrEqual));

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42, It.Is<string>(s => s.Contains("Alert removed")), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_RemoveAlert_InvalidId_SendsErrorMessage()
    {
        var update = CreateTextUpdate("/remove_alert not-a-guid", fromUserId: 200);

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("Invalid alert ID")),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_RemoveAlert_AccessDenied_SendsAccessDenied()
    {
        var alertId = Guid.NewGuid();
        var update = CreateTextUpdate($"/remove_alert {alertId}", fromUserId: 200);
        var user = new TrackedUser(42, 200, "testuser");
        var otherUser = new TrackedUser(99, 999, "other");

        _userRepositoryMock
            .Setup(r => r.GetByTelegramAsync(42, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _alertRepositoryMock
            .Setup(r => r.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PriceAlert(otherUser.Id, "BTC", "bitcoin", 70000m, AlertCondition.GreaterOrEqual));

        await _handler.HandleAsync(update);

        _messageSenderMock.Verify(m => m.SendMessageAsync(
            42,
            It.Is<string>(s => s.Contains("not found") || s.Contains("denied")),
            It.IsAny<CancellationToken>()));
    }

    private static Update CreateTextUpdate(string text, long fromUserId = 42)
    {
        return new Update
        {
            Id = 1,
            Message = new Message
            {
                MessageId = 1,
                Text = text,
                Chat = new Chat { Id = 42, Type = ChatType.Private },
                From = new User { Id = fromUserId },
                Date = DateTime.UtcNow
            }
        };
    }
}
