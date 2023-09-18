using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;

/// <summary>
/// Represents the GetMap request which returns an image of the map as byte[]
/// </summary>
public record GetMapImage : CorrelatedBy<Guid>
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public Guid CorrelationId { get; }

    /// <summary>
    /// <inheritdoc cref="GetMapImage"/>
    /// </summary>
    /// <param name="correlationId"></param>
    public GetMapImage(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}