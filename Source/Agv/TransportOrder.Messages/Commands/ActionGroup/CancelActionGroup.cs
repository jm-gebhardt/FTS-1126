namespace TransportOrder.Messages.Commands.ActionGroup;

/// <summary>
/// Provides a command to cancel an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record CancelActionGroup(Guid CorrelationId);