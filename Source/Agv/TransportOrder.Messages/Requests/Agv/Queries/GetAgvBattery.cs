using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;

/// <summary>
/// Represents the battery request which returns the battery status.
/// </summary>
/// <param name="CorrelationId">The CorrelationId of the Agv.</param>
public record GetAgvBattery(Guid CorrelationId) : CorrelatedBy<Guid>;