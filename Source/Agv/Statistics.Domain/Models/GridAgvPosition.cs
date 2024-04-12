using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.Statistics.Domain.Models;

/// <summary>
/// Represents an agv position on a grid.
/// </summary>
public class GridAgvPosition
{
    /// <summary>
    /// The unique id of the <see cref="GridAgvPosition"/>.
    /// </summary>
    public Guid PositionId { get; init; }

    /// <summary>
    /// The id of the grid the position is on.
    /// </summary>
    public Guid GridId { get; init; }
    
    /// <summary>
    /// The Grid object.
    /// </summary>
    public Grid? Grid { get; init; }

    /// <summary>
    /// The index of the grid cell in x direction.
    /// </summary>
    public int XIndex { get; init; }

    /// <summary>
    /// The index of the cell in y-direction.
    /// </summary>
    public int YIndex { get; init; }

    /// <summary>
    /// The x position.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// The y position.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// The agv serial number.
    /// </summary>
    public string SerialNumber
    {
        [PublicAPI]
        get;
        init;
    }

    /// <summary>
    /// The time the agv was at this position.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The list of AgvMapPositions object.
    /// </summary>
    [UsedImplicitly]
    public ICollection<MapAgvPositions>? MapAgvPositions { get; init; }

    /// <inheritdoc cref="GridAgvPosition"/>>
    public GridAgvPosition(Guid gridId, string serialNumber, float x, float y, int xIndex, int yIndex, DateTime time, Grid? grid)
    {
        PositionId = Guid.NewGuid();
        GridId = gridId;
        SerialNumber = serialNumber;
        X = x;
        Y = y;
        XIndex = xIndex;
        YIndex = yIndex;
        Timestamp = time;
        Grid = grid;
        MapAgvPositions = new List<MapAgvPositions>();
    }

    [UsedImplicitly]
    private GridAgvPosition()
    {
        PositionId = Guid.NewGuid();
        SerialNumber = string.Empty;
    }
}