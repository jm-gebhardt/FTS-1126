using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a request to set a new target for an action.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="ActionCorrelationId">The correlation id of the action.</param>
/// <param name="TargetNodeId">The new target node id.</param>
/// <param name="User">The user who set the new target node.</param>
public record TriggerSetTarget(Guid CorrelationId, Guid ActionCorrelationId, Guid TargetNodeId, string User) : CorrelatedBy<Guid>;