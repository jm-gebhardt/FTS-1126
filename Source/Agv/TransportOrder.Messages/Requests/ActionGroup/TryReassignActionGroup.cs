namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to try to reassign and action group to a different agv.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record TryReassignActionGroup(Guid CorrelationId);