using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to reset an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="KeepAgv">Whether to keep the same agv.</param>
public record ResetActionGroup(Guid CorrelationId, bool KeepAgv) : CorrelatedBy<Guid>;