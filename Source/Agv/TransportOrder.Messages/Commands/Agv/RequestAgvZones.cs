using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the EmitRequestZones request which returns AgvZones
/// </summary>
public record RequestAgvZones : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <inheritdoc cref="RequestAgvZones"/>
    public RequestAgvZones(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}