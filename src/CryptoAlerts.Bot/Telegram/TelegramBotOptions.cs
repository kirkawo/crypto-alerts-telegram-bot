namespace CryptoAlerts.Bot.Telegram;

public class TelegramBotOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; set; } = string.Empty;
}
