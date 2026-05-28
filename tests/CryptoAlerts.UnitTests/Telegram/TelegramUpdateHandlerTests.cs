using CryptoAlerts.Application.Dtos;
using CryptoAlerts.Application.Exceptions;
using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Application.Services;
using CryptoAlerts.Bot.Telegram;
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
    private readonly Mock<ITelegramMessageSender> _messageSenderMock;
    private readonly TelegramUpdateHandler _handler;

    public TelegramUpdateHandlerTests()
    {
        _priceProviderMock = new Mock<IPriceProvider>();
        _symbolResolverMock = new Mock<ISymbolResolver>();
        _messageSenderMock = new Mock<ITelegramMessageSender>();

        var priceQueryService = new PriceQueryService(
            _symbolResolverMock.Object, _priceProviderMock.Object);

        var loggerMock = new Mock<ILogger<TelegramUpdateHandler>>();
        var optionsMock = new Mock<IOptions<TelegramBotOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new TelegramBotOptions { BotUsername = "" });

        _handler = new TelegramUpdateHandler(
            priceQueryService, _messageSenderMock.Object, loggerMock.Object, optionsMock.Object);
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

    private static Update CreateTextUpdate(string text)
    {
        return new Update
        {
            Id = 1,
            Message = new Message
            {
                MessageId = 1,
                Text = text,
                Chat = new Chat { Id = 42, Type = ChatType.Private },
                Date = DateTime.UtcNow
            }
        };
    }
}
