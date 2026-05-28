using System.Globalization;
using CryptoAlerts.Application.Exceptions;
using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Application.Services;
using CryptoAlerts.Bot.Commands;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CryptoAlerts.Bot.Telegram;

public class TelegramUpdateHandler
{
    private readonly PriceQueryService _priceQueryService;
    private readonly ITelegramMessageSender _messageSender;
    private readonly ILogger<TelegramUpdateHandler> _logger;

    private const string WelcomeMessage =
        "Welcome to CryptoAlertsBot! Use /help to see available commands.";

    private const string HelpMessage =
        "Available commands:\n" +
        "/price <symbol> - Get current price\n" +
        "/start - Show welcome message\n" +
        "/help - Show this help";

    private const string UnknownMessage =
        "Unknown command. Use /help to see available commands.";

    private const string PriceUsageMessage =
        "Usage: /price <symbol> (e.g., /price BTC)";

    private const string SupportedSymbols = "Supported symbols: BTC, ETH, SOL, BNB, XRP, ADA, DOGE";

    public TelegramUpdateHandler(
        PriceQueryService priceQueryService,
        ITelegramMessageSender messageSender,
        ILogger<TelegramUpdateHandler> logger)
    {
        _priceQueryService = priceQueryService;
        _messageSender = messageSender;
        _logger = logger;
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update.Type != UpdateType.Message || update.Message is not { Text: { } text })
            return;

        var chatId = update.Message.Chat.Id;

        try
        {
            var command = CommandParser.Parse(text);

            switch (command.Type)
            {
                case CommandType.Start:
                    await _messageSender.SendMessageAsync(chatId, WelcomeMessage, cancellationToken);
                    break;

                case CommandType.Help:
                    await _messageSender.SendMessageAsync(chatId, HelpMessage, cancellationToken);
                    break;

                case CommandType.Price:
                    await HandlePriceCommand(chatId, command, cancellationToken);
                    break;

                default:
                    await _messageSender.SendMessageAsync(chatId, UnknownMessage, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from chat {ChatId}", chatId);
        }
    }

    private async Task HandlePriceCommand(long chatId, ParsedCommand command, CancellationToken ct)
    {
        if (command.Arguments.Count == 0)
        {
            await _messageSender.SendMessageAsync(chatId, PriceUsageMessage, ct);
            return;
        }

        var symbol = command.Arguments[0];

        try
        {
            var price = await _priceQueryService.GetPriceAsync(symbol, "usd", ct);
            var formattedValue = price.Value.ToString("0.########", CultureInfo.InvariantCulture);
            var message = $"{price.AssetSymbol}: {formattedValue} {price.Currency.ToUpperInvariant()}";
            await _messageSender.SendMessageAsync(chatId, message, ct);
        }
        catch (UnknownSymbolException)
        {
            await _messageSender.SendMessageAsync(
                chatId, $"Unknown symbol: '{symbol}'. {SupportedSymbols}", ct);
        }
    }
}
