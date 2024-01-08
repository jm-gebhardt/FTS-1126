using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides a command to trigger abortion of an action group.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
/// <param name="AbortReason">The reason for the abort.</param>
public record TriggerActionGroupAbortion(Guid CorrelationId, AbortReason AbortReason);