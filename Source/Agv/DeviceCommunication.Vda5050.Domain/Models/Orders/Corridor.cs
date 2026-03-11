using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

/// <summary>
/// Definition of boundaries in which a vehicle can deviate from its trajectory.
/// </summary>
[PublicAPI]
public record Corridor
{
    /// <summary>
    /// Width of the corridor in meters to the left of the vehicle's trajectory.
    /// </summary>
    public double LeftWidth { get; set; }

    /// <summary>
    /// Width of the corridor in meters to the right of the vehicle's trajectory.
    /// </summary>
    public double RightWidth { get; set; }

    /// <summary>
    /// Defines whether the boundaries are valid for the kinematic center or the contour of the vehicle.
    /// Optional.
    /// </summary>
    public CorridorRefPoint? CorridorRefPoint { get; set; }
}

/// <summary>
/// Reference point for corridor boundary calculations.
/// </summary>
[PublicAPI]
public enum CorridorRefPoint
{
    /// <summary>Boundaries are valid for the kinematic center of the vehicle.</summary>
    KinematicCenter,

    /// <summary>Boundaries are valid for the contour of the vehicle.</summary>
    Contour
}
