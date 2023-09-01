using MassTransit;

namespace TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Represents the GetAvgStatus request which returns AgvState
/// </summary>
[JetBrains.Annotations.UsedImplicitly]
public record GetAgvStatus : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// <inheritdoc cref="GetAgvStatus"/>
    /// </summary>
    /// <param name="correlationId"></param>
    public GetAgvStatus(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}