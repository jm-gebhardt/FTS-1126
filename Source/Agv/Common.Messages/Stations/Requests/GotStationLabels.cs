namespace Gebhardt.Acc.Common.Messages.Stations.Requests;

/// <summary>
/// Represents the response to the <see cref="GetStationLabels"/> request.
/// </summary>
/// <param name="Labels">All lanes/names used for stations.</param>
public record GotStationLabels(HashSet<string>Labels);