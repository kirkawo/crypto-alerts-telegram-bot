using CryptoAlerts.Domain.Entities;

namespace CryptoAlerts.Application.Interfaces;

public interface IAlertRepository
{
    Task AddAsync(PriceAlert alert, CancellationToken cancellationToken = default);
    Task UpdateAsync(PriceAlert alert, CancellationToken cancellationToken = default);
    Task<PriceAlert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken = default);
    Task<List<PriceAlert>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<PriceAlert>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
