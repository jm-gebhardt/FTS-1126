using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when a charging node was selected for an agv,
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="ChargeStationId">The id of the charger.</param>
/// <param name="Initiator">The initiator of the charging request.</param>
public record ChargingNodeSelectedForAgv(Guid CorrelationId, string ChargeStationId, ActionGroupCreator Initiator = ActionGroupCreator.TransportOrder) : CorrelatedBy<Guid>;