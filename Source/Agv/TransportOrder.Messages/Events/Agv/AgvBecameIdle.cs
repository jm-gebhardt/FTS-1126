using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Agv became idle
/// </summary>
[PublicAPI]
public record AgvBecameIdle(Guid CorrelationId) : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; } = CorrelationId;
}