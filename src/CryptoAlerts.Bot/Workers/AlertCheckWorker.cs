using CryptoAlerts.Application.Services;
using Microsoft.Extensions.Options;

namespace CryptoAlerts.Bot.Workers;

public class AlertCheckWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<AlertCheckWorkerOptions> _options;
    private readonly ILogger<AlertCheckWorker> _logger;

    public AlertCheckWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<AlertCheckWorkerOptions> options,
        ILogger<AlertCheckWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Alert check worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<AlertProcessingService>();
                var triggered = await service.ProcessAlertsAsync(stoppingToken);

                if (triggered > 0)
                    _logger.LogInformation("Triggered {Count} alert(s)", triggered);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alert check cycle failed");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.Value.PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Alert check worker stopped");
    }
}
