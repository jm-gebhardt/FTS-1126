using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;

/// <summary>
/// Point of reference for the location of a bounding box in AGV coordinate system.
/// </summary>
[PublicAPI]
public record MqttBoundingBoxReference
{
    /// <summary>X coordinate in meters.</summary>
    public double X { get; set; }

    /// <summary>Y coordinate in meters.</summary>
    public double Y { get; set; }

    /// <summary>Z coordinate in meters.</summary>
    public double Z { get; set; }

    /// <summary>
    /// Orientation of the load's bounding box. Important for tugger trains, etc.
    /// </summary>
    public double? Theta { get; set; }
}

/// <summary>
/// Dimensions of a load's bounding box.
/// </summary>
[PublicAPI]
public record MqttLoadDimensions
{
    /// <summary>Absolute length of the load's bounding box in meters.</summary>
    public double Length { get; set; }

    /// <summary>Absolute width of the load's bounding box in meters.</summary>
    public double Width { get; set; }

    /// <summary>
    /// Absolute height of the load's bounding box in meters.
    /// Optional: set value only if known.
    /// </summary>
    public double? Height { get; set; }
}
