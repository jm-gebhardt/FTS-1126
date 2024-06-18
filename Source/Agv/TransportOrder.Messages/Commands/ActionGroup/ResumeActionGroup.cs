using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to resume a paused action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="Setter">User that resumes the action group.</param>
public record ResumeActionGroup(Guid CorrelationId, string? Setter) : CorrelatedBy<Guid>;