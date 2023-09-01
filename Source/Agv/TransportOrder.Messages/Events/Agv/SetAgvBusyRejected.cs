using MassTransit;

// ReSharper disable ConvertToPrimaryConstructor
namespace TransportOrder.Messages.Events.Agv;

/// <summary>
/// Agv reservation request was rejected
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record SetAgvBusyRejected : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc cref="SetAgvBusyRejected"/>>
    /// <param name="correlationId"></param>
    public SetAgvBusyRejected(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}