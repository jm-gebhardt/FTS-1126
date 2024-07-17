namespace Gebhardt.Acc.KaeserSidecar.Domain.Models;

/// <summary>
/// The station occupancy.
/// </summary>
/// <param name="Station">The station id.</param>
/// <param name="Count">The agv count.</param>
public record StationOccupancy(string Station, int Count);