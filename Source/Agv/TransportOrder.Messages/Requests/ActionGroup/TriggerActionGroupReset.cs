namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a request to trigger resetting an action group.
/// </summary>
/// <param name="CorrelationId">The action group correlation id.</param>
/// <param name="KeepAgv">Whether to keep the same agv.</param>
public record TriggerActionGroupReset(Guid CorrelationId, bool KeepAgv);