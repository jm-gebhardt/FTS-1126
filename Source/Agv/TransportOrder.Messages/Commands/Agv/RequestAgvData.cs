using MassTransit;

namespace TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the EmitRequestData request
/// </summary>
public record RequestAgvData : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// <inheritdoc cref="RequestAgvData"/>
    /// </summary>
    /// <param name="correlationId"></param>
    public RequestAgvData(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}