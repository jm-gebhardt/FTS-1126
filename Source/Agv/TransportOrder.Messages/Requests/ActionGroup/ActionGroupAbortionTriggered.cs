namespace Gebhardt.Acc.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Provides the response to the <see cref="TriggerActionGroupAbortion"/> request.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record ActionGroupAbortionTriggered(Guid CorrelationId);