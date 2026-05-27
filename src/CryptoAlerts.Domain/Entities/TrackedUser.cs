namespace CryptoAlerts.Domain.Entities;

public class TrackedUser
{
    public Guid Id { get; private set; }
    public long TelegramChatId { get; private set; }
    public long TelegramUserId { get; private set; }
    public string? Username { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private TrackedUser() { }

    public TrackedUser(long telegramChatId, long telegramUserId, string? username)
    {
        Id = Guid.NewGuid();
        TelegramChatId = telegramChatId;
        TelegramUserId = telegramUserId;
        Username = username;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
