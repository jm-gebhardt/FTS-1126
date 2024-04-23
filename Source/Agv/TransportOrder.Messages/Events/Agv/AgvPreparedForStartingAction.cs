using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised after the agv was prepared for starting the action.
/// </summary>
/// <param name="CorrelationId">The agv correlation id.</param>
/// <param name="Action">The action.</param>
public record AgvPreparedForStartingAction(Guid CorrelationId, AgvAction Action) : CorrelatedBy<Guid>;