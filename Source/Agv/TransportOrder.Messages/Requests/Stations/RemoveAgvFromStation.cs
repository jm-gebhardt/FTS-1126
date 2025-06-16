namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Represents a request to remove an agv from a station.
/// </summary>
/// <param name="StationName">The station name.</param>
/// <param name="AgvId">The agv id.</param>
public record RemoveAgvFromStation(string StationName, Guid AgvId);