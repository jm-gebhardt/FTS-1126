using MassTransit;
using ActionGroupCreator = Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.ActionGroupCreator;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to create a charging <see cref="ActionGroup"/> for an agv.
/// </summary>
/// <param name="CorrelationId">The correlationId of the charging action group.</param>
/// <param name="AgvCorrelationId">The correlationId of the agv.</param>
/// <param name="ChargeStationId">Charger id.</param>
/// <param name="Creator">The creator of the charge action group.</param>
public record StartChargingActionGroup(Guid CorrelationId, Guid AgvCorrelationId, string ChargeStationId, ActionGroupCreator Creator = ActionGroupCreator.TransportOrder): CorrelatedBy<Guid>;