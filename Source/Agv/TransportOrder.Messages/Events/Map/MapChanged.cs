namespace Gebhardt.Acc.TransportOrder.Messages.Events.Map;

/// <summary>
/// Represents the event raised when the map changed.
/// </summary>
/// <param name="Map">The updated map information.</param>
/// <param name="HasBackgroundChanged">Indicates whether the background of the map has changed.</param>
public record MapChanged(Requests.Map.Map Map, bool HasBackgroundChanged);