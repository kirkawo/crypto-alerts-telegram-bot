using CryptoAlerts.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoAlerts.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PriceQueryService>();
        services.AddScoped<AlertCommandService>();

        return services;
    }
}
