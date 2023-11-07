namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a command to trigger abortion of an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record TriggerActionGroupAbortion(Guid CorrelationId);