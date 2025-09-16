using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when a parking node was selected for an agv.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="ParkingNode">The parking node.</param>
public record ParkingNodeSelectedForAgv(Guid CorrelationId, GraphNode ParkingNode) : CorrelatedBy<Guid>;