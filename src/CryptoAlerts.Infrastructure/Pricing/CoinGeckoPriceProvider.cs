using System.Net.Http.Json;
using System.Text.Json;
using CryptoAlerts.Application.Dtos;
using CryptoAlerts.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace CryptoAlerts.Infrastructure.Pricing;

public class CoinGeckoPriceProvider : IPriceProvider
{
    private readonly HttpClient _httpClient;
    private readonly CoinGeckoOptions _options;

    public CoinGeckoPriceProvider(HttpClient httpClient, IOptions<CoinGeckoOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PriceResult> GetCurrentPriceAsync(
        string assetId, string currency, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/v3/simple/price?ids={assetId}&vs_currencies={currency}",
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(
            cancellationToken: cancellationToken);

        if (doc is null)
            throw new InvalidOperationException(
                $"CoinGecko returned an empty response for asset '{assetId}'.");

        if (!doc.RootElement.TryGetProperty(assetId, out var assetElement) ||
            !assetElement.TryGetProperty(currency, out var priceElement))
        {
            throw new InvalidOperationException(
                $"CoinGecko response did not contain expected data for asset '{assetId}' in currency '{currency}'.");
        }

        return new PriceResult
        {
            AssetSymbol = assetId,
            AssetId = assetId,
            Currency = currency,
            Value = priceElement.GetDecimal(),
            RetrievedAtUtc = DateTime.UtcNow
        };
    }
}
