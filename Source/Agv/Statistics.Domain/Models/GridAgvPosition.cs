using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Gebhardt.Acc.Statistics.Domain.Models;

/// <summary>
/// Represents an agv position on a grid.
/// </summary>
public class GridAgvPosition : ITimestamped
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
    /// The Z position [m].
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// The agv serial number.
    /// </summary>
    public string SerialNumber { get; init; }

    /// <inheritdoc />
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The weight of this position compared to other positions. Set by the compression.
    /// </summary>
    [PublicAPI]
    public int Weight { get; } = 1;

    /// <summary>
    /// The list of AgvMapPositions object.
    /// </summary>
    public ICollection<MapAgvPositions>? MapAgvPositions { get; init; }

    /// <summary>
    /// The list of AgvError object.
    /// </summary>
    [UsedImplicitly]
    public ICollection<AgvError>? AgvErrors { get; set; }

    /// <summary>
    /// The list of AgvFieldViolation object.
    /// </summary>
    [UsedImplicitly]
    public ICollection<AgvFieldViolation>? AgvFieldViolations { get; set; }

    /// <summary>
    /// The list of Wi-Fi metrics.
    /// </summary>
    [UsedImplicitly]
    public ICollection<AgvWifiMetrics>? AgvWifiMetrics { get; set; }

    /// <inheritdoc cref="GridAgvPosition"/>>
    public GridAgvPosition(Guid gridId, string serialNumber, float x, float y, float z, int xIndex, int yIndex, DateTime time, Grid? grid)
    {
        PositionId = Guid.NewGuid();
        GridId = gridId;
        SerialNumber = serialNumber;
        X = x;
        Y = y;
        Z = z;
        XIndex = xIndex;
        YIndex = yIndex;
        Timestamp = time;
        Grid = grid;
        MapAgvPositions = new List<MapAgvPositions>();
        AgvErrors = new List<AgvError>();
        AgvFieldViolations = new List<AgvFieldViolation>();
    }

    /// <inheritdoc cref="GridAgvPosition"/>>
    public GridAgvPosition(Guid positionId, Guid gridId, string serialNumber, float x, float y, float z, int xIndex, int yIndex, DateTime timeStamp, Grid? grid, List<MapAgvPositions> mapAgvPositions, List<AgvError> agvErrors, List<AgvFieldViolation> agvFieldViolations)
    {
        PositionId = positionId;
        GridId = gridId;
        SerialNumber = serialNumber;
        X = x;
        Y = y;
        Z = z;
        XIndex = xIndex;
        YIndex = yIndex;
        Timestamp = timeStamp;
        Grid = grid;
        MapAgvPositions = mapAgvPositions;
        AgvErrors = agvErrors;
        AgvFieldViolations = agvFieldViolations;
    }

    /// <inheritdoc cref="GridAgvPosition"/>>
    [JsonConstructor]
    [UsedImplicitly]
    public GridAgvPosition()
    {
        PositionId = Guid.NewGuid();
        SerialNumber = string.Empty;
    }
}