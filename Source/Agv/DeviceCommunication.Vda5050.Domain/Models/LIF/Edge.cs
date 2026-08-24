using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helper;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

using CommonEdge = Common.Domain.Models.NewGraph.Edge;

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

    /// <summary>
    /// Generates a <see cref="Common.Domain.Models.NewGraph.Edge"/> from this lif edge.
    /// </summary>
    /// <param name="edgeId">The id of the graph edge.</param>
    /// <param name="levelId">The level id.</param>
    /// <param name="nodes">The nodes of the graph.</param>
    /// <param name="layoutId">The layout id.</param>
    /// <param name="projectIdentifier">The project identifier.</param>
    public CommonEdge ToGraphEdge(Guid edgeId, string? levelId, Dictionary<Guid, Common.Domain.Models.NewGraph.Node> nodes, string layoutId, string projectIdentifier)
    {
        bool hasStartNode = nodes.TryGetValue(LifHelper.GenerateInternalId(StartNodeId, layoutId, projectIdentifier), out var graphStartNode);
        bool hasEndNode = nodes.TryGetValue(LifHelper.GenerateInternalId(EndNodeId, layoutId, projectIdentifier), out var graphEndNode);
        if (!hasStartNode || !hasEndNode)
        {
            // This should have already been caught in lif validation
            throw new ArgumentException($"Node {(hasStartNode ? EndNodeId : StartNodeId)} referenced in edge {layoutId} - {EdgeId} does not exist.");
        }

        // Valid null suppression because if one of the nodes is null the previous exception would have been thrown.
        return new CommonEdge(edgeId, null, null, graphStartNode!.Id, graphEndNode!.Id);
    }
}
