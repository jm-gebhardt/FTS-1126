using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Represents the request which returns list of unresolved AgvError
/// </summary>
public record GetActiveAgvErrors : CorrelatedBy<Guid>
{
    /// <inheritdoc cref="GetActiveAgvErrors" />
    /// <param name="agvCorrelationId"></param>
    public GetActiveAgvErrors(Guid agvCorrelationId)
    {
        CorrelationId = agvCorrelationId;
    }

    /// <summary>
    /// serialization ctor
    /// </summary>
    [UsedImplicitly]
    public GetActiveAgvErrors()
    {
    }

    /// <summary>
    /// Correlation Id of Agv
    /// </summary>
    [UsedImplicitly]
    public Guid CorrelationId { get; init; }
}