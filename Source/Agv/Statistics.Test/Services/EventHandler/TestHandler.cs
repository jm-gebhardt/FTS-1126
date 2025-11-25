using Gebhardt.Acc.Statistics.Domain.Models;
using Gebhardt.Acc.Statistics.Domain.Repositories;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Gebhardt.Acc.Statistics.Test.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Test.Services.EventHandler;

public class TestHandler(
    IStatisticsDb statisticsDb,
    IPublishEndpoint publishEndpoint,
    ILogger<TestHandler> logger
) : GridCellAttachedHandlerBase<TestMessage>(statisticsDb, publishEndpoint, logger)
{
    public readonly List<(TestMessage, List<GridCell>)> HandledMessages = [];

    protected override Task HandleAsync(TestMessage message, List<GridCell> gridCells)
    {
        HandledMessages.Add((message, gridCells));
        return Task.CompletedTask;
    }
}