using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to update an action group using the agv that runs an idle action.
/// </summary>
/// <param name="CorrelationId">The action group correlation id.</param>
public record TryUpdateIdleAction(Guid CorrelationId): CorrelatedBy<Guid>;