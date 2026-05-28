using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Infrastructure.Persistence;
using CryptoAlerts.Infrastructure.Persistence.Repositories;
using CryptoAlerts.Infrastructure.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CryptoAlerts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();

        services.Configure<CoinGeckoOptions>(
            configuration.GetSection(CoinGeckoOptions.SectionName));

        services.AddHttpClient<IPriceProvider, CoinGeckoPriceProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<CoinGeckoOptions>>();
            client.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
        });

        services.AddSingleton<ISymbolResolver, StaticSymbolResolver>();

        return services;
    }
}
