using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to try to reassign and action group to a different agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="AssignedAgv">The correlation id of the new assigned agv, if any.</param>
public record TryReassignActionGroup(Guid CorrelationId, Guid? AssignedAgv) : CorrelatedBy<Guid>;