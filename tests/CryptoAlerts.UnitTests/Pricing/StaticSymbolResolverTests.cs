using CryptoAlerts.Application.Exceptions;
using CryptoAlerts.Infrastructure.Pricing;

namespace CryptoAlerts.UnitTests.Pricing;

public class StaticSymbolResolverTests
{
    private readonly StaticSymbolResolver _sut = new();

    [Theory]
    [InlineData("BTC", "bitcoin")]
    [InlineData("ETH", "ethereum")]
    [InlineData("SOL", "solana")]
    [InlineData("BNB", "binancecoin")]
    [InlineData("XRP", "ripple")]
    [InlineData("ADA", "cardano")]
    [InlineData("DOGE", "dogecoin")]
    public async Task Resolve_KnownSymbol_ReturnsCoinGeckoId(string symbol, string expectedId)
    {
        var result = await _sut.ResolveAssetIdAsync(symbol);

        Assert.Equal(expectedId, result);
    }

    [Theory]
    [InlineData("btc")]
    [InlineData("Btc")]
    [InlineData("bTC")]
    [InlineData("eth")]
    [InlineData("sOl")]
    public async Task Resolve_CaseInsensitive_ReturnsCoinGeckoId(string symbol)
    {
        var result = await _sut.ResolveAssetIdAsync(symbol);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Resolve_UnknownSymbol_ThrowsUnknownSymbolException()
    {
        var ex = await Assert.ThrowsAsync<UnknownSymbolException>(
            () => _sut.ResolveAssetIdAsync("XYZ"));

        Assert.Equal("XYZ", ex.Symbol);
    }

    [Fact]
    public async Task Resolve_EmptyString_ThrowsUnknownSymbolException()
    {
        await Assert.ThrowsAsync<UnknownSymbolException>(
            () => _sut.ResolveAssetIdAsync(""));
    }
}
