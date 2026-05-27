using CryptoAlerts.Application.Dtos;
using CryptoAlerts.Application.Exceptions;
using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Domain.Entities;
using CryptoAlerts.Domain.Enums;

namespace CryptoAlerts.Application.Services;

public class AlertCommandService
{
    private readonly IUserRepository _userRepository;
    private readonly IAlertRepository _alertRepository;
    private readonly ISymbolResolver _symbolResolver;

    public AlertCommandService(
        IUserRepository userRepository,
        IAlertRepository alertRepository,
        ISymbolResolver symbolResolver)
    {
        _userRepository = userRepository;
        _alertRepository = alertRepository;
        _symbolResolver = symbolResolver;
    }

    public async Task<AlertListItem> CreateAlertAsync(CreateAlertRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByTelegramAsync(
            request.TelegramChatId, request.TelegramUserId, cancellationToken);

        if (user is null)
        {
            user = new TrackedUser(request.TelegramChatId, request.TelegramUserId, request.Username);
            await _userRepository.AddAsync(user, cancellationToken);
        }

        var assetId = await _symbolResolver.ResolveAssetIdAsync(request.AssetSymbol, cancellationToken);

        var alert = new PriceAlert(user.Id, request.AssetSymbol, assetId, request.TargetPrice, AlertCondition.GreaterOrEqual);
        await _alertRepository.AddAsync(alert, cancellationToken);

        return MapToListItem(alert);
    }

    public async Task<List<AlertListItem>> GetUserAlertsAsync(
        long telegramChatId, long telegramUserId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByTelegramAsync(
            telegramChatId, telegramUserId, cancellationToken);

        if (user is null)
            return new List<AlertListItem>();

        var alerts = await _alertRepository.GetByUserIdAsync(user.Id, cancellationToken);

        return alerts.Select(MapToListItem).ToList();
    }

    public async Task CancelAlertAsync(
        Guid alertId, long telegramChatId, long telegramUserId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByTelegramAsync(
            telegramChatId, telegramUserId, cancellationToken);

        if (user is null)
            throw new AlertAccessDeniedException();

        var alert = await _alertRepository.GetByIdAsync(alertId, cancellationToken);

        if (alert is null || alert.UserId != user.Id)
            throw new AlertAccessDeniedException();

        alert.Cancel();
    }

    private static AlertListItem MapToListItem(PriceAlert alert)
    {
        return new AlertListItem
        {
            AlertId = alert.Id,
            AssetSymbol = alert.AssetSymbol,
            TargetPrice = alert.TargetPrice,
            Status = alert.Status,
            CreatedAtUtc = alert.CreatedAtUtc
        };
    }
}
