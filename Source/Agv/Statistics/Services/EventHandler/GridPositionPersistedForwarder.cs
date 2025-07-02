using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.Statistics.Messages.Events;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Statistics.Services.EventHandler;

/// <summary>
/// Forwards <see cref="GridPositionPersisted"/> events to the matching handler for <typeparamref name="TMessage"/>.
/// This is needed because we can't have multiple IConsumer implementations with generic type parameters
///  — the C# compiler (CS0695) complains due to possible type overlap, even if it actually can't happen because of our constraints.
/// So this class exists just to cleanly route those events to the right handler without breaking type safety.
/// </summary>
[UsedImplicitly]
public class GridPositionPersistedForwarder<TMessage> : IConsumer<GridPositionPersisted>
    where TMessage : class, IAgvSerialNumber, IWithAgvPosition, IMessageCreatedDate
{
    private readonly GridPositionAttachedHandlerBase<TMessage> _handler;

    /// <inheritdoc cref="GridPositionPersistedForwarder{TMessage}"/>
    public GridPositionPersistedForwarder(GridPositionAttachedHandlerBase<TMessage> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public Task Consume(ConsumeContext<GridPositionPersisted> context) => _handler.OnGridPositionPersisted(context.Message);
}