using CryptoAlerts.Application.Dtos;
using CryptoAlerts.Application.Exceptions;
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
        try
        {
            var assetId = await _symbolResolver.ResolveAssetIdAsync(symbol, cancellationToken);

            // Attach the original symbol to the result so callers get back what they requested
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
        catch (UnknownSymbolException)
        {
            throw;
        }
    }
}
