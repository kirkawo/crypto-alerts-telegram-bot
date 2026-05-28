using CryptoAlerts.Domain.Entities;

namespace CryptoAlerts.Application.Interfaces;

public interface IUserRepository
{
    Task<TrackedUser?> GetByTelegramAsync(long telegramChatId, long telegramUserId, CancellationToken cancellationToken = default);
    Task AddAsync(TrackedUser user, CancellationToken cancellationToken = default);
    Task<TrackedUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
