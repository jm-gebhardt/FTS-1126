using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;

/// <summary>
/// Represents the request which returns list of unresolved AgvError
/// </summary>
public record GetActiveAgvErrorsRequest : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="GetActiveAgvErrorsRequest" />
    /// <param name="agvCorrelationId"></param>
    public GetActiveAgvErrorsRequest(Guid agvCorrelationId)
    {
        CorrelationId = agvCorrelationId;
    }

    /// <summary>
    /// serialization ctor
    /// </summary>
    [UsedImplicitly]
    public GetActiveAgvErrorsRequest()
    {
        
    }

    /// <summary>
    /// Correlation Id of Agv
    /// </summary>
    [UsedImplicitly]
    public Guid CorrelationId { get; init; }
}