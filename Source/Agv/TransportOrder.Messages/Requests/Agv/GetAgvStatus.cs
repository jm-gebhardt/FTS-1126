using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;

/// <summary>
/// Represents the GetAvgStatus request which returns AgvState
/// </summary>
[UsedImplicitly]
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