namespace CryptoAlerts.Application.Interfaces;

public interface ISymbolResolver
{
    Task<string> ResolveAssetIdAsync(string symbol, CancellationToken cancellationToken = default);
}
