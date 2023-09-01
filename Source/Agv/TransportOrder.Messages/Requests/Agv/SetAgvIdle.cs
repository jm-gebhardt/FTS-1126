using MassTransit;

namespace TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Request an AGV to transition to idle state
/// </summary>
[JetBrains.Annotations.PublicAPI]
public record SetAgvIdle : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// Build message.
    /// </summary>
    /// <param name="correlationId"></param>
    public SetAgvIdle(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}