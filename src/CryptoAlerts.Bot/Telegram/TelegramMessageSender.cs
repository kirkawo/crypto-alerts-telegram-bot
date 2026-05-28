using CryptoAlerts.Application.Interfaces;
using Telegram.Bot;

namespace CryptoAlerts.Bot.Telegram;

public class TelegramMessageSender : ITelegramMessageSender
{
    private readonly ITelegramBotClient _botClient;

    public TelegramMessageSender(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public async Task SendMessageAsync(long chatId, string message, CancellationToken cancellationToken = default)
    {
        await _botClient.SendTextMessageAsync(chatId, message, cancellationToken: cancellationToken);
    }
}
