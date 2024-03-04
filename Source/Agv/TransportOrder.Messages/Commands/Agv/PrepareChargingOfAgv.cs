using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to prepare the charging of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Initiator">The initiator of the command.</param>
public record PrepareChargingOfAgv(Guid CorrelationId, ActionGroupCreator Initiator = ActionGroupCreator.TransportOrder): CorrelatedBy<Guid>;