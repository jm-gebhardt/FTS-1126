namespace Gebhardt.StoreWare.Agv.Statistics.Domain.Models;

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
}