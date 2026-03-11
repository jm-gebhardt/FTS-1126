using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// Position of a node on a map in world coordinates, as specified in an order.
/// </summary>
[PublicAPI]
public record OrderNodePosition
{
    /// <summary>X-position on the map in reference to the map coordinate system.</summary>
    public double X { get; set; }

    /// <summary>Y-position on the map in reference to the map coordinate system.</summary>
    public double Y { get; set; }

    /// <summary>
    /// Absolute orientation of the AGV on the node in radians [-π, π].
    /// Optional: vehicle can plan the path by itself.
    /// </summary>
    public double? Theta { get; set; }

    /// <summary>
    /// Allowed deviation radius in meters for the AGV to count the node as traversed.
    /// 0 means no deviation is allowed beyond normal AGV manufacturer tolerance.
    /// Optional.
    /// </summary>
    public double? AllowedDeviationXy { get; set; }

    /// <summary>
    /// Maximum allowed deviation of the theta angle in radians [0, π].
    /// Optional.
    /// </summary>
    public double? AllowedDeviationTheta { get; set; }

    /// <summary>
    /// Unique identification of the map in which the position is referenced.
    /// </summary>
    public string MapId { get; set; } = string.Empty;

    /// <summary>
    /// Additional information on the map.
    /// </summary>
    public string? MapDescription { get; set; }
}