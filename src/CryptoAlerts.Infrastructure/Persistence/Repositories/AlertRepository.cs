using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoAlerts.Infrastructure.Persistence.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly AppDbContext _context;

    public AlertRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(PriceAlert alert, CancellationToken cancellationToken = default)
    {
        await _context.PriceAlerts.AddAsync(alert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PriceAlert alert, CancellationToken cancellationToken = default)
    {
        _context.PriceAlerts.Update(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PriceAlert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        return await _context.PriceAlerts.FindAsync(new object[] { alertId }, cancellationToken);
    }

    public async Task<List<PriceAlert>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.PriceAlerts
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
