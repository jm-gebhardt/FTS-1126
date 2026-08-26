using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// An edge.
/// </summary>
[PublicAPI]
public class Edge
{
    /// <inheritdoc cref="Edge"/>
    public Edge(string edgeId, string endNodeId, string startNodeId, ICollection<VehicleTypeEdgeProperty> vehicleTypeEdgeProperties)
    {
        EdgeId = edgeId;
        EndNodeId = endNodeId;
        StartNodeId = startNodeId;
        VehicleTypeEdgeProperties = vehicleTypeEdgeProperties;
    }

    /// <inheritdoc cref="Edge"/>
    public Edge()
    {
    }

    /// <summary>
    /// Unique identifier for the edge.
    /// </summary>
    public string EdgeId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the ending node for this edge.
    /// </summary>
    public string EndNodeId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the starting node for this edge.
    /// </summary>
    public string StartNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Vehicle-specific properties for the edge.
    /// </summary>
    public ICollection<VehicleTypeEdgeProperty> VehicleTypeEdgeProperties { get; set; } = [];

    /// <summary>
    /// Get vehicle types supported by the edge.
    /// </summary>
    /// <returns>VehicleTypeIds included in the edge.</returns>
    public ICollection<string> GetSupportedVehicleTypes()
    {
        return VehicleTypeEdgeProperties.Select(v => v.VehicleTypeId).Distinct().ToList();
    }

    /// <summary>
    /// Get properties for given vehicle type.
    /// </summary>
    /// <param name="vehicleTypeId">Vehicle type id.</param>
    public VehicleTypeEdgeProperty? GetPropertiesForVehicle(string vehicleTypeId)
    {
        return VehicleTypeEdgeProperties.SingleOrDefault(v => v.VehicleTypeId == vehicleTypeId);
    }

    /// <summary>
    /// Get the edge id for a vehicle type.
    /// </summary>
    /// <param name="vehicleType">The vehicle type.</param>
    /// <returns>The fleet manager edge id for this vehicle type.</returns>
    public Guid? GetFleetManagerEdgeIdForVehicle(string vehicleType)
    {
        return VehicleTypeEdgeProperties.SingleOrDefault(v => v.VehicleTypeId == vehicleType)?.FleetManagerEdgeId;
    }
}
