using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to create a parking action group.
/// </summary>
/// <param name="CorrelationId">The action group correlation id.</param>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
/// <param name="ParkingNode">The parking node.</param>
public record CreateParkingActionGroup(Guid CorrelationId, Guid AgvCorrelationId, GraphNode ParkingNode):CorrelatedBy<Guid>;