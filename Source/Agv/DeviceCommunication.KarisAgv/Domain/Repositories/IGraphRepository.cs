using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;

/// <summary>
/// Repository for GraphNodes and GraphEdges and their matching MqttNodes and MqttEdges.
/// Stations have no corresponding MqttNodes and edges to stations have no corresponding MqttEdges.
/// </summary>
public interface IGraphRepository
{
    /// <summary>
    /// Retrieve the MqttNode by GraphNode id.
    /// </summary>
    /// <param name="graphNodeId">The nodeId from GraphNode.</param>
    /// <returns>Null if there is no information yet for the given graphNodeId</returns>
    public Task<MqttNode?> GetMqttNode(string graphNodeId);

    /// <summary>
    /// Get the mqtt nodes corresponding to the given graph node ids.
    /// </summary>
    /// <param name="graphNodeIds">The graph node ids.</param>
    /// <returns>A collection of mqtt nodes.</returns>
    public Task<ICollection<MqttNode>> GetMqttNodes(string[] graphNodeIds);

    /// <summary>
    /// Retrieve the MqttEdge by GraphEdge id.
    /// </summary>
    /// <param name="graphEdgeId">The nodeId from GraphNode.</param>
    /// <returns>Null if there is no information yet for the given graphEdgeId</returns>
    public Task<MqttEdge?> GetMqttEdge(Guid graphEdgeId);

    /// <summary>
    /// Get the mqtt edges corresponding to the given graph edge ids.
    /// </summary>
    /// <param name="graphEdgeIds">The graph node ids.</param>
    /// <returns>A collection of mqtt edges.</returns>
    public Task<ICollection<MqttEdge>> GetMqttEdges(Guid[] graphEdgeIds);

    /// <summary>
    /// Adds MqttNode to the repository.
    /// </summary>
    /// <param name="graphNodeId">The nodeId from GraphNode.</param>
    /// <param name="mqttNode">The mqtt node.</param>
    public Task Add(string graphNodeId, MqttNode mqttNode);

    /// <summary>
    /// Adds MqttNode to the repository.
    /// </summary>
    /// <param name="graphEdgeId">The edgeId from GraphNode.</param>
    /// <param name="mqttEdge">The mqtt edge.</param>
    public Task Add(Guid graphEdgeId, MqttEdge mqttEdge);

    /// <summary>
    /// Adds multiple MqttNodes to the repository.
    /// </summary>
    /// <param name="edges">The edges to add</param>
    public Task Add(params (Guid GraphEdgeId, MqttEdge MqttEdge)[] edges);

    /// <summary>
    /// Adds multiple MqttNodes and MqttEdges to the repository.
    /// </summary>
    /// <param name="nodesById">The nodes to add, indexed by their nodeId.</param>
    /// <param name="edgesById">The edges to add, indexed by their edgeId.</param>
    /// <returns></returns>
    Task Add(Dictionary<string, MqttNode> nodesById, Dictionary<Guid, MqttEdge> edgesById);

    /// <summary>
    /// Load cache.
    /// </summary>
    public Task LoadCache();
}