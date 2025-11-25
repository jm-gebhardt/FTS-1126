using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Handles the reception of AGV Wi-Fi metrics events.
/// </summary>
[UsedImplicitly]
public class WifiMetricsEventHandler : GridCellAttachedHandlerBase<AgvWifiMetricsReceived>
{
    private readonly IMessageFilter<AgvWifiMetricsReceived> _filter;

    /// <inheritdoc />
    public WifiMetricsEventHandler(
        IStatisticsDb statisticsDb,
        IPublishEndpoint publishEndpoint,
        IMessageFilter<AgvWifiMetricsReceived> filter,
        ILogger<WifiMetricsEventHandler> logger)
        : base(statisticsDb, publishEndpoint, logger)
    {
        _filter = filter;
    }

    /// <inheritdoc />
    protected override async Task HandleAsync(AgvWifiMetricsReceived message, List<GridCell> gridCells)
    {
        if (!await _filter.ShouldActAsync(message))
        {
            return;
        }

        await PublishEndpoint.Publish(new AddWifiMetrics(message.CorrelationId, message.Metrics, gridCells.Select(g => g.CellId).ToList()));
    }
}