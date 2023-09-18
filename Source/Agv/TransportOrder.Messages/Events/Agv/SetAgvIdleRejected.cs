using JetBrains.Annotations;
using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Agv reservation request was rejected
/// </summary>
[PublicAPI]
public record SetAgvIdleRejected : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc cref="SetAgvIdleRejected"/>>
    /// <param name="correlationId"></param>
    public SetAgvIdleRejected(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}