using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;

/// <summary>
/// Vehicle-type-specific properties for a <see cref="GraphLayoutEdge"/>.
/// </summary>
[PublicAPI]
public record EdgeVehicleTypeProperty
{
    /// <summary>
    /// The vehicle type this property applies to (e.g. "Manufacturer.SeriesName").
    /// </summary>
    public string VehicleTypeId { get; init; } = string.Empty;

    /// <summary>
    /// The agv orientation [rad].
    /// </summary>
    public double? VehicleOrientation { get; init; }

    /// <summary>
    /// The orientation type.
    /// </summary>
    public OrientationType OrientationType { get; init; }

    /// <summary>
    /// True: rotation is allowed on the edge. False: rotation is not allowed on the edge.
    /// Optional: no limit if not set.
    /// </summary>
    public bool? RotationAllowed { get; init; }

    /// <summary>
    /// Maximum rotation speed in rad/s. Optional: no limit if not set.
    /// </summary>
    public double? MaxRotationSpeed { get; init; }

    /// <summary>
    /// Permitted maximum speed on the edge in m/s. Optional: no limit if not set.
    /// </summary>
    public double? MaxSpeed { get; init; }

    /// <summary>
    /// Permitted maximum height of the vehicle including the load on the edge in meters.
    /// Optional: no limit if not set.
    /// </summary>
    public double? MaxHeight { get; init; }

    /// <summary>
    /// Permitted minimum height of the load handling device on the edge in meters.
    /// Optional: no limit if not set.
    /// </summary>
    public double? MinHeight { get; init; }

    /// <summary>
    /// The trajectory the agv drives when running on this edge.
    /// </summary>
    public Trajectory? Trajectory { get; init; }

    /// <summary>
    /// Actions defined for this vehicle type on this edge.
    /// </summary>
    public ICollection<EdgeVehicleTypeAction> Actions { get; init; } = [];
}