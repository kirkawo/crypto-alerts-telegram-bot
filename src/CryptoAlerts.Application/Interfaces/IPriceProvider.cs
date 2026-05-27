using CryptoAlerts.Application.Dtos;

namespace CryptoAlerts.Application.Interfaces;

public interface IPriceProvider
{
    Task<PriceResult> GetCurrentPriceAsync(string assetId, string currency, CancellationToken cancellationToken = default);
}
