using CryptoAlerts.Application.Dtos;
using CryptoAlerts.Application.Interfaces;

namespace CryptoAlerts.Application.Services;

public class PriceQueryService
{
    private readonly ISymbolResolver _symbolResolver;
    private readonly IPriceProvider _priceProvider;

    public PriceQueryService(ISymbolResolver symbolResolver, IPriceProvider priceProvider)
    {
        _symbolResolver = symbolResolver;
        _priceProvider = priceProvider;
    }

    public async Task<PriceResult> GetPriceAsync(string symbol, string currency, CancellationToken cancellationToken = default)
    {
        var assetId = await _symbolResolver.ResolveAssetIdAsync(symbol, cancellationToken);

        var price = await _priceProvider.GetCurrentPriceAsync(assetId, currency, cancellationToken);

        return new PriceResult
        {
            AssetSymbol = symbol,
            AssetId = price.AssetId,
            Currency = price.Currency,
            Value = price.Value,
            RetrievedAtUtc = price.RetrievedAtUtc
        };
    }
}
