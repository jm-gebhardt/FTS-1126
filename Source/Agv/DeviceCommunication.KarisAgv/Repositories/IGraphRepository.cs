using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Repositories;

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
    public MqttNode? GetMqttNode(string graphNodeId);

    /// <summary>
    /// Retrieve the MqttEdge by GraphEdge id.
    /// </summary>
    /// <param name="graphEdgeId">The nodeId from GraphNode.</param>
    /// <returns>Null if there is no information yet for the given graphEdgeId</returns>
    public MqttEdge? GetMqttEdge(Guid graphEdgeId);

    /// <summary>
    /// Updates the repository with MqttNode.
    /// </summary>
    /// <param name="graphNodeId"></param>
    /// <param name="mqttNode"></param>
    public void Add(string graphNodeId, MqttNode mqttNode);

    /// <summary>
    /// Updates the repository with MqttEdge.
    /// </summary>
    /// <param name="graphEdgeId"></param>
    /// <param name="mqttEdge"></param>
    public void Add(Guid graphEdgeId, MqttEdge mqttEdge);
}