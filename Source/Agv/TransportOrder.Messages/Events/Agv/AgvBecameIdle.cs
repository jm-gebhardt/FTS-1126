using MassTransit;

namespace TransportOrder.Messages.Events.Agv;

/// <summary>
/// Agv became idle
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record AgvBecameIdle(Guid CorrelationId) : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; } = CorrelationId;
}