using JetBrains.Annotations;

namespace Statistics.Domain.Models;

/// <summary>
/// Represents an agv position on a grid.
/// </summary>
public class AgvGridPosition
{
    /// <summary>
    /// The unique id of the <see cref="AgvGridPosition"/>.
    /// </summary>
    public Guid PositionId { get; init; }

    /// <summary>
    /// The id of the grid the position is on.
    /// </summary>
    public Guid GridId { get; init; }

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
    /// The y position
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// The agv id.
    /// </summary>
    public string AgvId {get; init; }

    /// <summary>
    /// The time the agv was at this position.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <inheritdoc cref="AgvGridPosition"/>>
    public AgvGridPosition(Guid gridId, string agvId, float x, float y, int xIndex, int yIndex, DateTime time)
    {
        PositionId = Guid.NewGuid();
        GridId = gridId;
        AgvId = agvId;
        X = x;
        Y = y;
        XIndex = xIndex;
        YIndex = yIndex;
        Timestamp = time;
    }

    [UsedImplicitly]
    private AgvGridPosition()
    {
        PositionId = Guid.NewGuid();
        AgvId = string.Empty;
    }
}