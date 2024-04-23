using Gebhardt.Acc.Common.Messages.ActionGroups;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;

/// <summary>
/// Provides a command to prepare the charging of an agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the agv.</param>
/// <param name="Initiator">The initiator of the command.</param>
public record PrepareChargingOfAgv(Guid CorrelationId, ActionGroupCreator Initiator = ActionGroupCreator.TransportOrder): CorrelatedBy<Guid>;