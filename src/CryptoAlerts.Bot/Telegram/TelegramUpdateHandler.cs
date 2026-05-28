using System.Globalization;
using CryptoAlerts.Application.Dtos;
using CryptoAlerts.Application.Exceptions;
using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Application.Services;
using CryptoAlerts.Bot.Commands;
using Microsoft.Extensions.Options;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CryptoAlerts.Bot.Telegram;

public class TelegramUpdateHandler
{
    private readonly PriceQueryService _priceQueryService;
    private readonly AlertCommandService _alertCommandService;
    private readonly ITelegramMessageSender _messageSender;
    private readonly ILogger<TelegramUpdateHandler> _logger;
    private readonly string _botUsername;

    private const string WelcomeMessage =
        "Welcome to CryptoAlertsBot! Use /help to see available commands.";

    private const string HelpMessage =
        "Available commands:\n" +
        "/price <symbol> - Get current price\n" +
        "/set_alert <symbol> <price> - Set a price alert\n" +
        "/list_alerts - List your alerts\n" +
        "/remove_alert <alertId> - Remove an alert\n" +
        "/start - Show welcome message\n" +
        "/help - Show this help";

    private const string UnknownMessage =
        "Unknown command. Use /help to see available commands.";

    private const string PriceUsageMessage =
        "Usage: /price <symbol> (e.g., /price BTC)";

    private const string SetAlertUsageMessage =
        "Usage: /set_alert <symbol> <price> (e.g., /set_alert BTC 70000)";

    private const string RemoveAlertUsageMessage =
        "Usage: /remove_alert <alertId> (e.g., /remove_alert a1b2c3d4-e5f6-7890-abcd-ef1234567890)";

    private const string SupportedSymbols = "Supported symbols: BTC, ETH, SOL, BNB, XRP, ADA, DOGE";

    private const string GenericErrorMessage =
        "An unexpected error occurred. Please try again later.";

    public TelegramUpdateHandler(
        PriceQueryService priceQueryService,
        AlertCommandService alertCommandService,
        ITelegramMessageSender messageSender,
        ILogger<TelegramUpdateHandler> logger,
        IOptions<TelegramBotOptions> options)
    {
        _priceQueryService = priceQueryService;
        _alertCommandService = alertCommandService;
        _messageSender = messageSender;
        _logger = logger;
        _botUsername = options.Value.BotUsername;
    }

    public async Task HandleAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update.Type != UpdateType.Message || update.Message is not { Text: { } text } || update.Message.From is null)
            return;

        var chatId = update.Message.Chat.Id;
        var fromUserId = update.Message.From.Id;
        var fromUsername = update.Message.From.Username;

        try
        {
            var command = CommandParser.Parse(text, _botUsername);

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

                case CommandType.SetAlert:
                    await HandleSetAlertCommand(chatId, command, fromUserId, fromUsername, cancellationToken);
                    break;

                case CommandType.ListAlerts:
                    await HandleListAlertsCommand(chatId, fromUserId, cancellationToken);
                    break;

                case CommandType.RemoveAlert:
                    await HandleRemoveAlertCommand(chatId, command, fromUserId, cancellationToken);
                    break;

                default:
                    await _messageSender.SendMessageAsync(chatId, UnknownMessage, cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from chat {ChatId}", chatId);
            await _messageSender.SendMessageAsync(chatId, GenericErrorMessage, cancellationToken);
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

    private async Task HandleSetAlertCommand(long chatId, ParsedCommand command, long fromUserId, string? fromUsername, CancellationToken ct)
    {
        if (command.Arguments.Count < 2)
        {
            await _messageSender.SendMessageAsync(chatId, SetAlertUsageMessage, ct);
            return;
        }

        var symbol = command.Arguments[0];

        if (!decimal.TryParse(command.Arguments[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price <= 0)
        {
            await _messageSender.SendMessageAsync(chatId, "Invalid price. Use a positive number (e.g., 70000 or 0.05).", ct);
            return;
        }

        try
        {
            var request = new CreateAlertRequest
            {
                TelegramChatId = chatId,
                TelegramUserId = fromUserId,
                Username = fromUsername,
                AssetSymbol = symbol,
                TargetPrice = price
            };

            var result = await _alertCommandService.CreateAlertAsync(request, ct);
            var formattedPrice = result.TargetPrice.ToString("0.########", CultureInfo.InvariantCulture);
            await _messageSender.SendMessageAsync(chatId, $"Alert set: {result.AssetSymbol} at {formattedPrice} USD.", ct);
        }
        catch (UnknownSymbolException)
        {
            await _messageSender.SendMessageAsync(
                chatId, $"Unknown symbol: '{symbol}'. {SupportedSymbols}", ct);
        }
    }

    private async Task HandleListAlertsCommand(long chatId, long fromUserId, CancellationToken ct)
    {
        var alerts = await _alertCommandService.GetUserAlertsAsync(chatId, fromUserId, ct);

        if (alerts.Count == 0)
        {
            await _messageSender.SendMessageAsync(chatId, "You have no alerts set.", ct);
            return;
        }

        var lines = alerts.Select((a, i) =>
        {
            var formattedPrice = a.TargetPrice.ToString("0.########", CultureInfo.InvariantCulture);
            return $"{i + 1}. {a.AssetSymbol} @ {formattedPrice} USD - {a.Status} (id: {a.AlertId})";
        });

        await _messageSender.SendMessageAsync(chatId, $"Your alerts:\n{string.Join("\n", lines)}", ct);
    }

    private async Task HandleRemoveAlertCommand(long chatId, ParsedCommand command, long fromUserId, CancellationToken ct)
    {
        if (command.Arguments.Count == 0)
        {
            await _messageSender.SendMessageAsync(chatId, RemoveAlertUsageMessage, ct);
            return;
        }

        if (!Guid.TryParse(command.Arguments[0], out var alertId))
        {
            await _messageSender.SendMessageAsync(chatId, "Invalid alert ID format. Use a valid GUID.", ct);
            return;
        }

        try
        {
            await _alertCommandService.CancelAlertAsync(alertId, chatId, fromUserId, ct);
            await _messageSender.SendMessageAsync(chatId, "Alert removed.", ct);
        }
        catch (AlertAccessDeniedException)
        {
            await _messageSender.SendMessageAsync(chatId, "Alert not found or access denied.", ct);
        }
    }
}
