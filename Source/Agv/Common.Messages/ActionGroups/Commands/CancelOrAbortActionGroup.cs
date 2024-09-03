using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups.Commands;

/// <summary>
/// Provides a command to cancel an action group if it was not started or abort it if it was.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record CancelOrAbortActionGroup(Guid CorrelationId) : CorrelatedBy<Guid>;