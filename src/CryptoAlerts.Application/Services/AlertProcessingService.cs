using System.Globalization;
using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Domain.Entities;

namespace CryptoAlerts.Application.Services;

public class AlertProcessingService
{
    private readonly IAlertRepository _alertRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPriceProvider _priceProvider;
    private readonly ITelegramMessageSender _messageSender;

    public AlertProcessingService(
        IAlertRepository alertRepository,
        IUserRepository userRepository,
        IPriceProvider priceProvider,
        ITelegramMessageSender messageSender)
    {
        _alertRepository = alertRepository;
        _userRepository = userRepository;
        _priceProvider = priceProvider;
        _messageSender = messageSender;
    }

    public async Task<int> ProcessAlertsAsync(CancellationToken cancellationToken = default)
    {
        var alerts = await _alertRepository.GetAllActiveAsync(cancellationToken);
        var triggered = 0;

        foreach (var alert in alerts)
        {
            try
            {
                if (await TryTriggerAlertAsync(alert, cancellationToken))
                    triggered++;
            }
            catch
            {
                // Individual alert failure should not crash the cycle
            }
        }

        return triggered;
    }

    private async Task<bool> TryTriggerAlertAsync(PriceAlert alert, CancellationToken ct)
    {
        var price = await _priceProvider.GetCurrentPriceAsync(alert.AssetId, "usd", ct);

        if (price.Value < alert.TargetPrice)
            return false;

        var user = await _userRepository.GetByIdAsync(alert.UserId, ct);

        if (user is null)
            return false;

        var formattedCurrent = price.Value.ToString("0.########", CultureInfo.InvariantCulture);
        var message = $"Alert triggered: {alert.AssetSymbol} reached {alert.TargetPrice} USD (current: {formattedCurrent} USD)";

        await _messageSender.SendMessageAsync(user.TelegramChatId, message, ct);

        alert.Trigger();
        await _alertRepository.UpdateAsync(alert, ct);

        return true;
    }
}
