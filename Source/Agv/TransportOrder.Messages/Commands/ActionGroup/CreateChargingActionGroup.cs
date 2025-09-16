using Gebhardt.Acc.Common.Domain.Models.Graphs;
using MassTransit;
using ActionGroupCreator = Gebhardt.Acc.Common.Messages.ActionGroups.ActionGroupCreator;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to create a charging <see cref="ActionGroup"/> for an agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the charging action group.</param>
/// <param name="AgvCorrelationId">The correlationId of the agv.</param>
/// <param name="ChargeStation">Charger node.</param>
/// <param name="Creator">The creator of the charge action group.</param>
public record CreateChargingActionGroup(Guid CorrelationId, Guid AgvCorrelationId, GraphNode ChargeStation, ActionGroupCreator Creator = ActionGroupCreator.TransportOrder): CorrelatedBy<Guid>;