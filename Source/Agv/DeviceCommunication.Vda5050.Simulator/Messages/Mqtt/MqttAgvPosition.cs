using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.Mqtt;

/// <summary>
/// Current position of the AGV on a map in world coordinates.
/// </summary>
[PublicAPI]
public record MqttAgvPosition
{
    /// <summary>X coordinate in meters.</summary>
    public double X { get; set; }

    /// <summary>Y coordinate in meters.</summary>
    public double Y { get; set; }

    /// <summary>Orientation of the AGV in radians.</summary>
    public double Theta { get; set; }

    /// <summary>Unique identifier of the map the position refers to.</summary>
    public string MapId { get; set; } = string.Empty;

    /// <summary>Additional information on the map.</summary>
    public string? MapDescription { get; set; }

    /// <summary>
    /// True: position is initialized. False: position is not initialized.
    /// </summary>
    public bool PositionInitialized { get; set; }

    /// <summary>
    /// Describes the quality of the localization.
    /// 0.0: position unknown, 1.0: position known.
    /// Optional for vehicles that cannot estimate their localization score.
    /// </summary>
    public double? LocalizationScore { get; set; }

    /// <summary>
    /// Value for position deviation range in meters.
    /// Optional for vehicles that cannot estimate their deviation.
    /// </summary>
    public double? DeviationRange { get; set; }
}

/// <summary>
/// The AGV's velocity in vehicle coordinates.
/// </summary>
[PublicAPI]
public record MqttVelocity
{
    /// <summary>The AGV's velocity in its x direction.</summary>
    public double? Vx { get; set; }

    /// <summary>The AGV's velocity in its y direction.</summary>
    public double? Vy { get; set; }

    /// <summary>The AGV's turning speed around its z axis.</summary>
    public double? Omega { get; set; }
}
