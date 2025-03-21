using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the EmitRequestData request
/// </summary>
public record RequestAgvData : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc cref="RequestAgvData"/>
    public RequestAgvData(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}