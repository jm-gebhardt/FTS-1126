using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to pause an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record PauseActionGroup(Guid CorrelationId) : CorrelatedBy<Guid>;