using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoAlerts.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TrackedUser?> GetByTelegramAsync(
        long telegramChatId, long telegramUserId, CancellationToken cancellationToken = default)
    {
        return await _context.TrackedUsers
            .FirstOrDefaultAsync(
                u => u.TelegramChatId == telegramChatId && u.TelegramUserId == telegramUserId,
                cancellationToken);
    }

    public async Task AddAsync(TrackedUser user, CancellationToken cancellationToken = default)
    {
        await _context.TrackedUsers.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<TrackedUser?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.TrackedUsers.FindAsync(new object[] { userId }, cancellationToken);
    }
}
