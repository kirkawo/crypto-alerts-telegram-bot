namespace CryptoAlerts.Bot.Configuration;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddLocalOverrides(
        this IConfigurationBuilder configuration,
        IHostEnvironment environment)
    {
        return configuration
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.MachineName}.json", optional: true, reloadOnChange: true);
    }
}