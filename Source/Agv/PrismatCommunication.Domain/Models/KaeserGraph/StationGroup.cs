namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;

/// <summary>
/// A station group
/// </summary>
public class StationGroup
{
    private readonly ICollection<Station> _stations;

    /// <summary>
    /// The station sources
    /// </summary>
    public ICollection<string> Sources => _stations.Select(s => s.Source).ToList();

    /// <summary>
    /// The station targets
    /// </summary>
    public ICollection<string> Targets => _stations.Select(s => s.Target).ToList();

    /// <summary>
    /// The id of the group.
    /// </summary>
    public Guid? GroupId => _stations.FirstOrDefault()?.GroupId;

    /// <summary>
    /// True, if the station group is occupied.
    /// </summary>
    public bool IsOccupied => _stations.Any(s => s.Occupancy > 0);

    /// <inheritdoc cref="StationGroup"/>>
    /// <param name="stations"></param>
    public StationGroup(ICollection<Station> stations)
    {
        _stations = stations;
    }
}