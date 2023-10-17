using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Agv became idle
/// </summary>
[PublicAPI]
public record AgvBecameIdle(Guid CorrelationId) : CorrelatedBy<Guid>
{
    /// <summary>
    /// The CorrelationId of the Agv
    /// </summary>
    public Guid CorrelationId { get; } = CorrelationId;
}