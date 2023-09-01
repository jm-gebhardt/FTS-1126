using MassTransit;

namespace TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Represents the GetAgvGraph request which returns AgvGraph
/// </summary>
[JetBrains.Annotations.UsedImplicitly]
public record GetAgvGraph : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// <inheritdoc cref="GetAgvGraph"/>
    /// </summary>
    /// <param name="correlationId"></param>
    public GetAgvGraph(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}