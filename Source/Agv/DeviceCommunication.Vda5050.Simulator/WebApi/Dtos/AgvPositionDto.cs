namespace DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;

/// <summary>
/// Provides a dto for agv positions.
/// </summary>
public record AgvPositionDto
{
    /// <summary>
    /// Returns true, when the position is known and valid.
    /// </summary>
    public bool PositionInitialized { get; init; }

    /// <summary>
    /// Current X-position [m].
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Current Y-position [m].
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// The mapId (vehicle map, not fleet manager map)
    /// </summary>
    public string MapId { get; init; } = string.Empty;

    /// <summary>
    /// Current Z-position [m].
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// Current angle (rotation) [rad].
    /// </summary>
    public float Theta { get; init; }

    /// <summary>
    /// Deviation from the responded position [m].
    /// </summary>
    public float? DeviationRange { get; init; }

    /// <summary>
    /// The quality of the localization and can therefore be used, e.g., by SLAM mobile robots to describe how accurate the current position information is [0..1];
    /// </summary>
    public double? LocalizationScore { get; init; }
}
