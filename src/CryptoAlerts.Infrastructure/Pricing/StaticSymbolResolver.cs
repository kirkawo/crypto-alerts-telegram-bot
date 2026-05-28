using CryptoAlerts.Application.Exceptions;
using CryptoAlerts.Application.Interfaces;

namespace CryptoAlerts.Infrastructure.Pricing;

public class StaticSymbolResolver : ISymbolResolver
{
    private static readonly Dictionary<string, string> SymbolMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTC"] = "bitcoin",
        ["ETH"] = "ethereum",
        ["SOL"] = "solana",
        ["BNB"] = "binancecoin",
        ["XRP"] = "ripple",
        ["ADA"] = "cardano",
        ["DOGE"] = "dogecoin",
    };

    public Task<string> ResolveAssetIdAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (SymbolMap.TryGetValue(symbol, out var assetId))
            return Task.FromResult(assetId);

        throw new UnknownSymbolException(symbol);
    }
}
