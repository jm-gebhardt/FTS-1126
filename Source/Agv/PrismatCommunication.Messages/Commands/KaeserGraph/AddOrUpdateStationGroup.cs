namespace Gebhardt.Acc.PrismatCommunication.Messages.Commands.KaeserGraph;

/// <summary>
/// Add or update stations of a station group.
/// </summary>
/// <param name="Stations">The stations of the group.</param>
/// <param name="GroupId">The group id.</param>
public record AddOrUpdateStationGroup(ICollection<Domain.Models.KaeserGraph.Station> Stations, Guid GroupId);