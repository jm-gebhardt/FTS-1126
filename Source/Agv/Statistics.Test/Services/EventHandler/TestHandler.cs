using Gebhardt.Acc.Statistics.Cache;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Gebhardt.Acc.Statistics.Test.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Test.Services.EventHandler;

public class TestHandler(
    IReadOnlyAgvScopedPositionCache<Guid> positionCache,
    IAgvScopedPositionCache<TestMessage> pendingCache,
    IPublishEndpoint publishEndpoint,
    ILogger<TestHandler> logger) : GridPositionAttachedHandlerBase<TestMessage>(positionCache, pendingCache, publishEndpoint, logger)
{
    public readonly List<(TestMessage, List<Guid>)> HandledMessages = [];

    protected override Task HandleWithGridIdsAsync(TestMessage message, List<Guid> gridIds)
    {
        HandledMessages.Add((message, gridIds));
        return Task.CompletedTask;
    }
}