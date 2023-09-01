using MassTransit;

namespace TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Represents the EmitRequestGraph request which returns AgvData
/// </summary>
public record RequestAgvGraph : CorrelatedBy<Guid>
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// <inheritdoc cref="RequestAgvGraph"/>
    /// </summary>
    /// <param name="correlationId"></param>
    public RequestAgvGraph(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}