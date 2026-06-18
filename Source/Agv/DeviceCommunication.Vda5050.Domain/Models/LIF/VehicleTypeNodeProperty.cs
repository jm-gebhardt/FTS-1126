using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// Property for a vehicle node type.
/// </summary>
[PublicAPI]
public class VehicleTypeNodeProperty
{
    /// <inheritdoc cref="VehicleTypeNodeProperty"/>
    public VehicleTypeNodeProperty(
        Guid fleetManagerNodeId,
        string vehicleTypeId,
        ICollection<VehicleTypeNodePropertyAction>? actions = null,
        double? theta = null)
    {
        FleetManagerNodeId = fleetManagerNodeId;
        VehicleTypeId = vehicleTypeId;
        Actions = actions;
        Theta = theta;
    }

    /// <inheritdoc cref="VehicleTypeNodeProperty"/>
    public VehicleTypeNodeProperty()
    {
    }

    /// <summary>
    /// The node id used in the fleet manager for the node of this vehicle type.
    /// </summary>
    public Guid FleetManagerNodeId { get; set; }

    /// <summary>
    /// List of actions that the vehicle can perform at the node. Optional.
    /// </summary>
    public ICollection<VehicleTypeNodePropertyAction>? Actions { get; set; }

    /// <summary>
    /// Absolute orientation of the vehicle on the node in reference to the global origin's rotation. Range: [-Pi ... Pi]
    /// </summary>
    public double? Theta { get; set; }

    /// <summary>
    /// Identifier for the vehicle type.
    /// </summary>
    public string VehicleTypeId { get; set; } = string.Empty;
}
