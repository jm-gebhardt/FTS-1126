using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Messages.Commands;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Handles the reception of AGV error events.
/// </summary>
[UsedImplicitly]
public class AgvErrorEventHandler : GridCellAttachedHandlerBase<AgvErrorReceived>
{
    /// <inheritdoc />
    public AgvErrorEventHandler(
        IStatisticsDb statisticsDb,
        IPublishEndpoint publishEndpoint,
        ILogger<AgvErrorEventHandler> logger)
        : base(statisticsDb, publishEndpoint, logger)
    {
    }

    /// <inheritdoc />
    protected override Task HandleAsync(AgvErrorReceived message, List<GridCell> gridCells)
    {
        if (message.Error.ErrorLevel == ErrorLevel.Information)
        {
            return Task.CompletedTask;
        }

        return PublishEndpoint.Publish(new AddError(message.Error, gridCells.Select(g => g.CellId).ToList()));
    }
}