using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace TransportOrder.Messages.Events.Agv;

/// <summary>
/// Agv reservation request was rejected
/// </summary>
[JetBrains.Annotations.PublicAPI]
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