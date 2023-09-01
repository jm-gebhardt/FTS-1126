using MassTransit;

namespace TransportOrder.Messages.Events.Agv;

/// <summary>
/// AGV reservation request was successful
/// </summary>
[JetBrains.Annotations.PublicAPI]
public sealed class SetAgvBusySucceeded : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// SetAgvBusySucceeded Constructor
    /// </summary>
    /// <param name="correlationId"></param>
    public SetAgvBusySucceeded(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}