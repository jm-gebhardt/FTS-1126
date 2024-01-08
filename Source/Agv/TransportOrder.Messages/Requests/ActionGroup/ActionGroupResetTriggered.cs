namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.ActionGroup;

/// <summary>
/// Represents the event raised when reset of an action group was triggered.
/// </summary>
/// <param name="CorrelationId">The correlation id of the action group.</param>
public record ActionGroupResetTriggered(Guid CorrelationId);