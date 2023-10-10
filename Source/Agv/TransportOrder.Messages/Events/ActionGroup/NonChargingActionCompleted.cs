namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when an action has been completed.
/// </summary>
/// <param name="AgvCorrelationId"></param>
public record NonChargingActionCompleted(Guid AgvCorrelationId);