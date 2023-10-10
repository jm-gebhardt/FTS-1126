namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.ActionGroup;

/// <summary>
/// Represents the event raised when starting the next action o a group failed.
/// </summary>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
public record StartingActionForAgvFailed(Guid AgvCorrelationId);