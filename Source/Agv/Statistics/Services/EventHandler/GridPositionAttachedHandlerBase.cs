using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.Statistics.Cache;
using Gebhardt.Acc.Statistics.Messages.Commands;
using Gebhardt.Acc.Statistics.Messages.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// An abstract base class that handles messages associated with grid positions.
/// Subclasses should implement the specific handling of a message for existing grid IDs.
/// </summary>
/// <typeparam name="TMessage"> The type of the message being handled.</typeparam>
public abstract class GridPositionAttachedHandlerBase<TMessage> :
    IConsumer<TMessage>
    where TMessage : class, IAgvSerialNumber, IWithAgvPosition, IMessageCreatedDate
{
    private readonly IReadOnlyAgvScopedPositionCache<Guid> _positionCache;
    private readonly IAgvScopedPositionCache<TMessage> _pendingCache;
    private readonly ILogger<GridPositionAttachedHandlerBase<TMessage>> _logger;

    /// <summary>MassTransit publish endpoint./// </summary>
    protected IPublishEndpoint PublishEndpoint { get; }

    /// <inheritdoc cref="GridPositionAttachedHandlerBase{TMessage}"/>
    protected GridPositionAttachedHandlerBase(
        IReadOnlyAgvScopedPositionCache<Guid> positionCache,
        IAgvScopedPositionCache<TMessage> pendingCache,
        IPublishEndpoint publishEndpoint,
        ILogger<GridPositionAttachedHandlerBase<TMessage>> logger
    )
    {
        _positionCache = positionCache;
        _pendingCache = pendingCache;
        _logger = logger;
        PublishEndpoint = publishEndpoint;
    }

    /// <summary>
    /// Handles a message with the already persisted grid IDs.
    /// </summary>
    /// <param name="message">The message object.</param>
    /// <param name="gridIds">A list of grid positions Ids which are already persisted in the database.</param>
    protected abstract Task HandleWithGridIdsAsync(TMessage message, List<Guid> gridIds);

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<TMessage> context)
    {
        var message = context.Message;

        if (_positionCache.TryGet(message.SerialNumber, message.AgvPosition, out List<Guid>? gridIds))
        {
            _logger.LogDebug("Handling {MessageType} for AGV {SerialNumber} with position {Position} and grid IDs {GridIds}",
                typeof(TMessage).Name, message.SerialNumber, message.AgvPosition, gridIds);

            await HandleWithGridIdsAsync(message, gridIds);
            return;
        }

        _pendingCache.AddItem(message.SerialNumber, message.AgvPosition, message);
        _logger.LogDebug("Adding to pending cache for AGV {SerialNumber} with position {Position} and sending force persist position", message.SerialNumber, message.AgvPosition);

        await PublishEndpoint.Publish(new ForcePersistAgvPosition(message.SerialNumber, message.AgvPosition)
        {
            MessageCreatedDate = message.MessageCreatedDate
        });
    }

    /// Handles the event when a grid position for an AGV is persisted.
    /// <param name="message">Information about the persisted grid position.</param>
    public async Task OnGridPositionPersisted(GridPositionPersisted message)
    {
        _logger.LogDebug("Received GridPositionPersisted for AGV {SerialNumber} with position {Position} and grid IDs {GridIds}",
            message.SerialNumber, message.AgvPosition, message.GridPositionIds);
        if (_pendingCache.TryRemove(message.SerialNumber, message.AgvPosition, out List<TMessage>? pending))
        {
            _logger.LogDebug("Found {Count} pending messages for AGV {SerialNumber} with position {Position}",
                pending.Count, message.SerialNumber, message.AgvPosition);
            foreach (var original in pending)
            {
                await HandleWithGridIdsAsync(original, message.GridPositionIds);
            }
        }
        else
        {
            _logger.LogDebug("Did not find any pending messages for AGV {SerialNumber} with position {Position}",
                message.SerialNumber, message.AgvPosition);
        }
    }
}