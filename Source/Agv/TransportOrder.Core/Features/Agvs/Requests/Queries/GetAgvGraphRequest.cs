using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;

/// <summary>
/// Represents the GetAgvGraph request which returns AgvGraph
/// </summary>
[JetBrains.Annotations.UsedImplicitly]
public record GetAgvGraphRequest : CorrelatedBy<Guid>
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; }

    /// <summary>
    /// <inheritdoc cref="GetAgvGraphRequest"/>
    /// </summary>
    /// <param name="correlationId"></param>
    public GetAgvGraphRequest(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}