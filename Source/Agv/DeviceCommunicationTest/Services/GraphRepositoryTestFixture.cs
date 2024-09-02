using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Repositories;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services;

// ReSharper disable ConvertConstructorToMemberInitializers
public class GraphRepositoryTestFixture
{
    public IGraphRepository GraphRepository { get; } = new GraphRepository();
 
    private static readonly MqttNode[] _mqttNodes =
    [
        new() { NodeId = "WP-001", X = -2f, Y = 3f, Z = 0f },
        new() { NodeId = "WP-002", X = 0f, Y = 3f, Z = 0f },
        new() { NodeId = "WP-003", X = 4f, Y = 3f, Z = 0f },
        new() { NodeId = "WP-004", X = 6f, Y = 3f, Z = 0f },
        new() { NodeId = "WP-005", X = 8f, Y = 3f, Z = 0f }
    ];

    private static readonly MqttEdge[] _mqttEdges =
    [
        new() { EdgeId = "E-1", StartId = _mqttNodes[0].NodeId, EndId = _mqttNodes[1].NodeId, Bidirectional = false },
        new() { EdgeId = "E-2", StartId = _mqttNodes[1].NodeId, EndId = _mqttNodes[2].NodeId, Bidirectional = false },
        new() { EdgeId = "E-3", StartId = _mqttNodes[2].NodeId, EndId = _mqttNodes[3].NodeId, Bidirectional = false },
        new() { EdgeId = "E-4", StartId = _mqttNodes[3].NodeId, EndId = _mqttNodes[4].NodeId, Bidirectional = true }
    ];

    private static readonly GraphNode[] _graphNodes =
    [
        NodeGenerator.GenerateNode(nodeId: _mqttNodes[0].NodeId, nodeType: AgvGraph.NodeType.WayPoint, x: _mqttNodes[0].X, y: _mqttNodes[0].Y, z: (float)_mqttNodes[0].Z!),
        NodeGenerator.GenerateNode(nodeId: _mqttNodes[1].NodeId, nodeType: AgvGraph.NodeType.WayPoint, x: _mqttNodes[1].X, y: _mqttNodes[1].Y, z: (float)_mqttNodes[1].Z!),
        NodeGenerator.GenerateNode(nodeId: _mqttNodes[2].NodeId, nodeType: AgvGraph.NodeType.WayPoint, x: _mqttNodes[2].X, y: _mqttNodes[2].Y, z: (float)_mqttNodes[2].Z!),
        NodeGenerator.GenerateNode(nodeId: _mqttNodes[3].NodeId, nodeType: AgvGraph.NodeType.WayPoint, x: _mqttNodes[3].X, y: _mqttNodes[3].Y, z: (float)_mqttNodes[3].Z!),
        NodeGenerator.GenerateNode(nodeId: _mqttNodes[4].NodeId, nodeType: AgvGraph.NodeType.WayPoint, x: _mqttNodes[4].X, y: _mqttNodes[4].Y, z: (float)_mqttNodes[4].Z!)
    ];

    private static readonly GraphEdge[] _graphEdges =
    [
        GraphEdge.CreateDirect(_mqttNodes[0].NodeId, _mqttNodes[1].NodeId, 2f, Guid.Parse("ed70292e-e001-e002-8112-ffe69119c4b0")),
        GraphEdge.CreateDirect(_mqttNodes[1].NodeId, _mqttNodes[2].NodeId, 2f, Guid.Parse("ed70292e-e002-e003-8112-ffe69119c4b0")),
        GraphEdge.CreateDirect(_mqttNodes[2].NodeId, _mqttNodes[3].NodeId, 4f, Guid.Parse("ed70292e-e003-e004-8112-ffe69119c4b0")),
        GraphEdge.CreateDirect(_mqttNodes[3].NodeId, _mqttNodes[4].NodeId, 2f, Guid.Parse("ed70292e-e004-e005-8112-ffe69119c4b0")),
        GraphEdge.CreateDirect(_mqttNodes[4].NodeId, _mqttNodes[3].NodeId, 2f, Guid.Parse("ed70292e-e005-e004-8112-ffe69119c4b0"))
    ];

    public Guid GetGraphEdgeId(int index)
    {
        if (index >= 0 && index < _graphEdges.Length)
        {
            return _graphEdges[index].Id;
        }
        return Guid.Empty;
    }

    public GraphRepositoryTestFixture WithGraphRepository()
    {
        for (int i = 0; i < _mqttNodes.Length; i++)
        {
            GraphRepository.Add(_graphNodes[i].Id, _mqttNodes[i]);
        }

        int j = 0;
        for (int i = 0; i < _graphEdges.Length; i++)
        {
            GraphRepository.Add(_graphEdges[i].Id, _mqttEdges[j]);
            if (_mqttEdges[j].Bidirectional)
            {
                GraphRepository.Add(_graphEdges[++i].Id, _mqttEdges[j]);
            }
            j++;
        }

        return this;
    }
}