namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to trigger resetting an action group.
/// </summary>
/// <param name="CorrelationId">The action group correlation id.</param>
public record TriggerActionGroupReset(Guid CorrelationId);