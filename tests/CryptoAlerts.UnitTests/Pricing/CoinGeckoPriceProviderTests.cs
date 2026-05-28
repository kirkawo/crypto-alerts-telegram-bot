using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CryptoAlerts.Infrastructure.Pricing;
using Microsoft.Extensions.Options;

namespace CryptoAlerts.UnitTests.Pricing;

public class CoinGeckoPriceProviderTests
{
    private readonly CoinGeckoOptions _options = new()
    {
        BaseUrl = "https://api.coingecko.com",
        DefaultCurrency = "usd"
    };

    private CoinGeckoPriceProvider CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_options.BaseUrl)
        };
        return new CoinGeckoPriceProvider(httpClient, Options.Create(_options));
    }

    [Fact]
    public async Task GetPriceAsync_ValidResponse_ReturnsPriceResult()
    {
        var json = """{"bitcoin":{"usd":45123.45}}""";
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var result = await provider.GetCurrentPriceAsync("bitcoin", "usd");

        Assert.Empty(result.AssetSymbol);
        Assert.Equal("bitcoin", result.AssetId);
        Assert.Equal("usd", result.Currency);
        Assert.Equal(45123.45m, result.Value);
        Assert.True(result.RetrievedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetPriceAsync_NullResponse_ThrowsInvalidOperationException()
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetCurrentPriceAsync("bitcoin", "usd"));

        Assert.Contains("bitcoin", ex.Message);
    }

    [Fact]
    public async Task GetPriceAsync_MissingAssetData_ThrowsInvalidOperationException()
    {
        var json = """{"someOtherCoin":{"usd":1.0}}""";
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });

        var provider = CreateProvider(handler);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetCurrentPriceAsync("bitcoin", "usd"));

        Assert.Contains("bitcoin", ex.Message);
    }

    [Fact]
    public async Task GetPriceAsync_NonSuccessStatusCode_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError,
        });

        var provider = CreateProvider(handler);
        await Assert.ThrowsAsync<HttpRequestException>(
            () => provider.GetCurrentPriceAsync("bitcoin", "usd"));
    }
}

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_response);
    }
}
