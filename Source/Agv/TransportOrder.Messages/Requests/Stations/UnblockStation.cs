namespace Gebhardt.Acc.TransportOrder.Messages.Requests.Stations;

/// <summary>
/// Unblocks the station.
/// </summary>
/// <param name="StationId">The station id to unblock.</param>
/// <param name="User">The user that requested the unblocking.</param>
public record UnblockStation(Guid StationId, string User);