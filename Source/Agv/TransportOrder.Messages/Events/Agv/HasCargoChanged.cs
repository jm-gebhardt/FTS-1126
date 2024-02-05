namespace Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;

/// <summary>
/// Represents the event raised when the agv cargo state
/// switches from loaded to unloaded or vice versa.
/// </summary>
/// <param name="AgvCorrelationId">The agv correlation id.</param>
/// <param name="HasCargo">True if the agv is loaded with cargo.</param>
public record HasCargoChanged(Guid AgvCorrelationId, bool HasCargo) ;