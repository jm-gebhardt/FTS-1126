using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// A node.
/// </summary>
[PublicAPI]
public class Node
{
    /// <inheritdoc cref="Node"/>
    public Node(
        string nodeId,
        NodePosition nodePosition,
        ICollection<VehicleTypeNodeProperty> vehicleTypeNodeProperties,
        string? mapId = null,
        string? nodeDescription = null,
        string? nodeName = null)
    {
        NodeId = nodeId;
        NodePosition = nodePosition;
        VehicleTypeNodeProperties = vehicleTypeNodeProperties;
        MapId = mapId;
        NodeDescription = nodeDescription;
        NodeName = nodeName;
    }

    /// <inheritdoc cref="Node"/>
    public Node()
    {
    }

    /// <summary>
    /// Identifier for the map that this node belongs to. Optional.
    /// </summary>
    public string? MapId { get; set; }

    /// <summary>
    /// Description of the node. Optional.
    /// </summary>
    public string? NodeDescription { get; set; }

    /// <summary>
    /// Unique identifier for the node.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the node. Optional.
    /// </summary>
    public string? NodeName { get; set; }

    /// <summary>
    /// Position of the node on the map (in meters).
    /// </summary>
    public NodePosition NodePosition { get; set; } = null!;

    /// <summary>
    /// Vehicle-specific properties related to the node.
    /// </summary>
    public ICollection<VehicleTypeNodeProperty> VehicleTypeNodeProperties { get; set; } = [];

    /// <summary>
    /// Get vehicle types supported by the node.
    /// </summary>
    /// <returns>VehicleTypeIds included in the node.</returns>
    public ICollection<string> GetSupportedVehicleTypes()
    {
        return VehicleTypeNodeProperties.Select(v => v.VehicleTypeId).Distinct().ToList();
    }

    /// <summary>
    /// Get properties for given vehicle type.
    /// </summary>
    /// <param name="vehicleTypeId">Vehicle type id.</param>
    public VehicleTypeNodeProperty? GetPropertiesForVehicle(string vehicleTypeId)
    {
        return VehicleTypeNodeProperties.SingleOrDefault(v => v.VehicleTypeId == vehicleTypeId);
    }

    /// <summary>
    /// Get all fleet manager node ids of this node.
    /// </summary>
    /// <returns>Array of Guids with the id.s</returns>
    public Guid[] GetFleetManagerNodeIds()
    {
        return VehicleTypeNodeProperties.Select(v => v.FleetManagerNodeId).ToArray();
    }

    /// <summary>
    /// Get the node id for a vehicle type.
    /// </summary>
    /// <param name="vehicleType">The vehicle type.</param>
    /// <returns>The fleet manager node id for this vehicle type.</returns>
    public Guid? GetFleetManagerNodeIdForVehicle(string vehicleType)
    {
        return VehicleTypeNodeProperties.SingleOrDefault(v => v.VehicleTypeId == vehicleType)?.FleetManagerNodeId;
    }
}
