using AutoMapper;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Statistics.Services.CommandHandler;

/// <summary>
/// Provides a handler for Wi-Fi metrics-related commands.
/// </summary>
[UsedImplicitly]
public class WifiMetricsCommandHandler(IStatisticsDb db, IMapper mapper) : IConsumer<AddWifiMetrics>
{
    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AddWifiMetrics> context)
    {
        (var agvCorrelationId, var wifiMetrics, List<Guid> gridCellIds) = context.Message;

        foreach (var gridCellId in gridCellIds)
        {
            var agvWifiMetrics = mapper.Map<AgvWifiMetrics>(wifiMetrics);
            agvWifiMetrics.AgvCorrelationId = agvCorrelationId;
            agvWifiMetrics.GridCellId = gridCellId;
            db.GridAgvWifiMetrics.Add(agvWifiMetrics);
        }

        await db.SaveAsync();
    }
}