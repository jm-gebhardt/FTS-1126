using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Graph;

/// <summary>
/// Request for the global graph together with the load states of all AGV types.
/// </summary>
/// <param name="CorrelationId">The identifier of this message.</param>
public record GetGraphOverview(Guid CorrelationId) : CorrelatedBy<Guid>;