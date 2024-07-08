using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using System.Collections.Concurrent;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Repositories;

/// <summary>
/// 
/// </summary>
public class GraphRepository : IGraphRepository
{
    private readonly ConcurrentDictionary<string, MqttNode> _nodeDictionary = new();
    private readonly ConcurrentDictionary<Guid, MqttEdge> _edgeDictionary = new();

    /// <inheritdoc />
    public MqttNode? GetMqttNode(string graphNodeId)
    {
        return _nodeDictionary.GetValueOrDefault(graphNodeId);
    }

    /// <inheritdoc />
    public MqttEdge? GetMqttEdge(Guid graphEdgeId)
    {
        return _edgeDictionary.GetValueOrDefault(graphEdgeId);
    }

    /// <inheritdoc />
    public void Add(string graphNodeId, MqttNode mqttNode)
    {
        _nodeDictionary.TryAdd(graphNodeId, mqttNode);
    }

    /// <inheritdoc />
    public void Add(Guid graphEdgeId, MqttEdge mqttEdge)
    {
        _edgeDictionary.TryAdd(graphEdgeId, mqttEdge);
    }
}