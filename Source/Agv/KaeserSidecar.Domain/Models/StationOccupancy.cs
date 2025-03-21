namespace Gebhardt.Acc.KaeserSidecar.Domain.Models;

/// <summary>
/// The station occupancy.
/// </summary>
public class StationOccupancy
{
    /// <summary>
    /// The station id.
    /// </summary>
    public string Station { get; init; }

    /// <summary>
    /// The agv count.
    /// </summary>
    public int Count { get; private set; }

    /// <inheritdoc cref="StationOccupancy"/>>
    public StationOccupancy(string station, int count)
    {
        Station = station;
        Count = count;
    }

    /// <summary>
    /// Add agv to station.
    /// </summary>
    public void Add()
    {
        Count += 1;
    }

    /// <summary>
    /// Remove agv from station.
    /// </summary>
    public void Remove()
    {
        if (Count == 0)
        {
            return;
        }
        Count -= 1;
    }
}