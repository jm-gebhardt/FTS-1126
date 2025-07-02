using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Statistics.Cache;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Handles the reception of AGV Wi-Fi metrics events.
/// </summary>
[UsedImplicitly]
public class WifiMetricsEventHandler : GridPositionAttachedHandlerBase<AgvWifiMetricsReceived>
{
    /// <inheritdoc />
    public WifiMetricsEventHandler(
        IReadOnlyAgvScopedPositionCache<Guid> positionCache,
        IAgvScopedPositionCache<AgvWifiMetricsReceived> pendingCache,
        IPublishEndpoint publishEndpoint,
        ILogger<WifiMetricsEventHandler> logger)
        : base(positionCache, pendingCache, publishEndpoint, logger)
    {
    }

    /// <inheritdoc />
    protected override Task HandleWithGridIdsAsync(AgvWifiMetricsReceived message, List<Guid> gridIds)
    {
        return PublishEndpoint.Publish(new AddWifiMetrics(message.Metrics, gridIds));
    }
}