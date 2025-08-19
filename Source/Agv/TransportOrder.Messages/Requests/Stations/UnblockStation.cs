namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Unblocks the station.
/// </summary>
/// <param name="StationId">The station id to unblock.</param>
public record UnblockStation(Guid StationId);