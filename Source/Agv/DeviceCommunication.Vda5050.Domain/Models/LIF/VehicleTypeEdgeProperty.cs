using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// Edge property for vehicle type.
/// </summary>
[PublicAPI]
public class VehicleTypeEdgeProperty
{
    /// <inheritdoc cref="VehicleTypeEdgeProperty"/>
    public VehicleTypeEdgeProperty(
        Guid fleetManagerEdgeId,
        string vehicleTypeId,
        ICollection<VehicleTypeEdgePropertyAction>? actions = null,
        LoadRestriction? loadRestriction = null,
        double? maxHeight = null,
        double? maxRotationSpeed = null,
        double? maxSpeed = null,
        double? minHeight = null,
        string? orientationType = null,
        bool? reentryAllowed = null,
        bool? rotationAllowed = null,
        string? rotationAtEndNodeAllowed = null,
        string? rotationAtStartNodeAllowed = null,
        Trajectory? trajectory = null,
        double? vehicleOrientation = null)
    {
        FleetManagerEdgeId = fleetManagerEdgeId;
        VehicleTypeId = vehicleTypeId;
        Actions = actions;
        LoadRestriction = loadRestriction;
        MaxHeight = maxHeight;
        MaxRotationSpeed = maxRotationSpeed;
        MaxSpeed = maxSpeed;
        MinHeight = minHeight;
        OrientationType = orientationType;
        ReentryAllowed = reentryAllowed;
        RotationAllowed = rotationAllowed;
        RotationAtEndNodeAllowed = rotationAtEndNodeAllowed;
        RotationAtStartNodeAllowed = rotationAtStartNodeAllowed;
        Trajectory = trajectory;
        VehicleOrientation = vehicleOrientation;
    }

    /// <inheritdoc cref="VehicleTypeEdgeProperty"/>
    public VehicleTypeEdgeProperty()
    {
    }

    /// <summary>
    /// The edge id used in the fleet manager for the node of this vehicle type.
    /// </summary>
    public Guid FleetManagerEdgeId { get; set; } = Guid.Empty;

    /// <summary>
    /// Actions that can be integrated into the order by the master control system each time any vehicle with the corresponding vehicleTypeId is sent an order containing this edge. Optional.
    /// </summary>
    public ICollection<VehicleTypeEdgePropertyAction>? Actions { get; set; }

    /// <summary>
    /// Load restrictions for this edge. Optional.
    /// </summary>
    public LoadRestriction? LoadRestriction { get; set; }

    /// <summary>
    /// Maximum height of the vehicle on this edge in meters. Optional. Range: [0.0 ... float64.max]
    /// </summary>
    public double? MaxHeight { get; set; }

    /// <summary>
    /// Maximum rotation speed allowed on this edge in radians per second. Optional. Range: [0.0 ... float64.max]
    /// </summary>
    public double? MaxRotationSpeed { get; set; }

    /// <summary>
    /// Maximum speed allowed on this edge in meters per second. Range: [0.0 ... float64.max]
    /// </summary>
    public double? MaxSpeed { get; set; }

    /// <summary>
    /// Minimum height of the vehicle on this edge in meters. Optional. Range: [0.0 ... float64.max]
    /// </summary>
    public double? MinHeight { get; set; }

    /// <summary>
    /// Type of orientation (e.g., TANGENTIAL).
    /// </summary>
    public string? OrientationType { get; set; }

    /// <summary>
    /// true: Vehicles are allowed to enter into automatic management while on this edge. false: Vehicles are not allowed. Default is true if not defined.
    /// </summary>
    public bool? ReentryAllowed { get; set; }

    /// <summary>
    /// Indicates if rotation is allowed while on the edge. Optional.
    /// </summary>
    public bool? RotationAllowed { get; set; }

    /// <summary>
    /// Specifies if rotation is allowed at the end node. Optional.
    /// </summary>
    public string? RotationAtEndNodeAllowed { get; set; }

    /// <summary>
    /// Specifies if rotation is allowed at the start node. Optional.
    /// </summary>
    public string? RotationAtStartNodeAllowed { get; set; }

    /// <summary>
    /// Trajectory information for this edge, if applicable. Optional.
    /// </summary>
    public Trajectory? Trajectory { get; set; }

    /// <summary>
    /// Orientation of the vehicle while traversing the edge, in radians.
    /// </summary>
    public double? VehicleOrientation { get; set; }

    /// <summary>
    /// Identifier for the vehicle type.
    /// </summary>
    public string VehicleTypeId { get; set; } = string.Empty;
}
