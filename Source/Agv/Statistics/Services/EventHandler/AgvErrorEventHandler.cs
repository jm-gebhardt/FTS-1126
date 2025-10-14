using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Statistics.Cache;
using Gebhardt.Acc.Statistics.Messages.Commands;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Handles the reception of AGV error events.
/// </summary>
public class AgvErrorEventHandler : GridPositionAttachedHandlerBase<AgvErrorReceived>
{
    /// <inheritdoc />
    public AgvErrorEventHandler(
        IReadOnlyAgvScopedPositionCache<Guid> positionCache,
        IAgvScopedPositionCache<AgvErrorReceived> pendingCache,
        IPublishEndpoint publishEndpoint,
        ILogger<AgvErrorEventHandler> logger)
        : base(positionCache, pendingCache, publishEndpoint, logger)
    {
    }

    /// <inheritdoc />
    protected override Task<bool> ShouldActAsync(AgvErrorReceived message) => Task.FromResult(true);

    /// <inheritdoc />
    protected override Task HandleWithGridIdsAsync(AgvErrorReceived message, List<Guid> gridIds)
    {
        return PublishEndpoint.Publish(new AddError(message.Error, gridIds));
    }
}