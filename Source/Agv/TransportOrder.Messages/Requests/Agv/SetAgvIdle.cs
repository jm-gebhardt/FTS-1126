using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Request an AGV to transition to idle state
/// </summary>
[PublicAPI]
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