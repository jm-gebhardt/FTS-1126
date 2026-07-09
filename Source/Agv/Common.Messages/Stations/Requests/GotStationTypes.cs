using Gebhardt.Acc.Common.Domain.Models.Stations;

namespace Gebhardt.Acc.Common.Messages.Stations.Requests;

/// <summary>
/// Represents the response to the <see cref="GetStationTypes"/> request.
/// </summary>
/// <param name="Stations">All stations with their node types.</param>
public record GotStationTypes(ICollection<StationTypeInfo> Stations);