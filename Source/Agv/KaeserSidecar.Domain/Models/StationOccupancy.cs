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

    /// <summary>
    /// True if the station does not have any agv (count).
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// The correlation id of the agv that occupies the station.
    /// </summary>
    public Guid? AgvCorrelationId { get; private set; }

    /// <inheritdoc cref="StationOccupancy"/>>
    public StationOccupancy(string station, int count, Guid? agvCorrelationId)
    {
        Station = station;
        Count = count;
        AgvCorrelationId = agvCorrelationId;
    }

    /// <summary>
    /// Add agv to station.
    /// </summary>
    public void Add(Guid agvCorrelationId)
    {
        if (AgvCorrelationId == agvCorrelationId)
        {
            return;
        }
        // Only increase occupancy for unoccupied stations,
        // as for target selection the actual number of vehicles is irrelevant.
        if (Count < 1)
        {
            Count += 1;
        }
        AgvCorrelationId = agvCorrelationId;
    }

    /// <summary>
    /// Remove agv from station.
    /// </summary>
    public void Remove()
    {
        AgvCorrelationId = null;
        if (Count == 0)
        {
            return;
        }
        Count -= 1;
    }
}