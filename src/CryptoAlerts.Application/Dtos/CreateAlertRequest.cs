namespace CryptoAlerts.Application.Dtos;

public class CreateAlertRequest
{
    public long TelegramChatId { get; init; }
    public long TelegramUserId { get; init; }
    public string? Username { get; init; }
    public string AssetSymbol { get; init; } = null!;
    public decimal TargetPrice { get; init; }
}
