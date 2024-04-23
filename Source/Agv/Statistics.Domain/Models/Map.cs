using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents a map
/// </summary>
public class Map(Guid mapId, Guid gridId, int xMin, int yMin, int xMax, int yMax)
{
    /// <summary>
    /// The id of the map.
    /// </summary>
    public Guid MapId { get; init; } = mapId;

    /// <summary>
    /// The id of the grid the map refers to.
    /// </summary>
    public Guid GridId { get; init; } = gridId;

    /// <summary>
    /// The minimum index of the map on the grid in x direction.
    /// </summary>
    public int XMin { get; private set; } = xMin;

    /// <summary>
    /// The minimum index of the map on the grid in y direction.
    /// </summary>
    public int YMin { get; private set; } = yMin;

    /// <summary>
    /// The maximum index of the map on the grid in x direction.
    /// </summary>
    public int YMax { get; private set; } = yMax;

    /// <summary>
    /// The maximum index of the map on the grid in y direction.
    /// </summary>
    public int XMax { get; private set; } = xMax;

    /// <summary>
    /// The Grid object.
    /// </summary>
    public Grid? Grid { get; init; }

    /// <summary>
    /// The list of AgvMapPositions object.
    /// </summary>
    [UsedImplicitly]
    public ICollection<MapAgvPositions>? MapAgvPositions { get; init; } = new List<MapAgvPositions>();

    /// <summary>
    /// Update the extent of a map.
    /// </summary>
    /// <param name="xMin">Minimum x index.</param>
    /// <param name="yMin">Minimum y index.</param>
    /// <param name="xMax">Maximum x index.</param>
    /// <param name="yMax">Minimum y index.</param>
    public void UpdateExtent(int xMin, int yMin, int xMax, int yMax)
    {
        XMin = Math.Min(xMin, XMin);
        XMax = Math.Max(xMax, XMax);
        YMin = Math.Min(yMin, YMin);
        YMax = Math.Max(yMax, YMax);
    }

    /// <summary>
    /// Check if position is within this map.
    /// </summary>
    /// <param name="xIndex">Position x index.</param>
    /// <param name="yIndex">Position y index.</param>
    /// <returns>boolean</returns>
    public bool IsPositionWithinMap(int xIndex, int yIndex)
    {
        bool isWithin = XMin <= xIndex && xIndex <= XMax &&
                        YMin <= yIndex && yIndex <= YMax;
        return isWithin;
    }
}