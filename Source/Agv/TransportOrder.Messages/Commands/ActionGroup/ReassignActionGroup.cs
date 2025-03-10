using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to re-assign an action group to a different agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record ReassignActionGroup(Guid CorrelationId): CorrelatedBy<Guid>;