using ActionGroupCreator = Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.ActionGroupCreator;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to create a charging <see cref="ActionGroup"/> for an agv.
/// </summary>
/// <param name="AgvCorrelationId">The correlationId of the agv.</param>
/// <param name="ChargeStationId">If set send agv to this charge station..</param>
/// <param name="Creator">The creator of the charge action group.</param>
public record StartChargingActionGroup(Guid AgvCorrelationId, string? ChargeStationId = null, ActionGroupCreator Creator = ActionGroupCreator.TransportOrder);