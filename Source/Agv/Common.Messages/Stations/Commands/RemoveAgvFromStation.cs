namespace Gebhardt.Acc.Common.Messages.Stations.Commands;

/// <summary>
/// Represents a command to remove an agv from a station via a different agv.
/// </summary>
/// <param name="StationId">The station to remove the agv from.</param>
/// <param name="AgvSerialNumber">The serial number of the agv to be removed.</param>
/// <param name="ActingAgvSerialNumber">The serial number of the acting agv.</param>
public record RemoveAgvFromStation(string StationId, string AgvSerialNumber, string ActingAgvSerialNumber);