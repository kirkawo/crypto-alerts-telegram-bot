namespace CryptoAlerts.Infrastructure.Pricing;

public class CoinGeckoOptions
{
    public const string SectionName = "CoinGecko";

    public string BaseUrl { get; set; } = "https://api.coingecko.com";

    public string DefaultCurrency { get; set; } = "usd";
}
