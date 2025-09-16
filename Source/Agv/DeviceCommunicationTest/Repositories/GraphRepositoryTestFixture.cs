using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Persistence.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Profiles;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.SemaphoreProvider;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gebhardt.Acc.DeviceCommunicationTest.Repositories;

// ReSharper disable ConvertConstructorToMemberInitializers
public class GraphRepositoryTestFixture : DeviceCommunicationDbTestFixture
{
    private readonly SemaphoreProvider<IGraphRepositorySemaphore> _semaphoreProvider = new(new Mock<ILogger<SemaphoreProvider<IGraphRepositorySemaphore>>>().Object);

    public IGraphRepository GraphRepository { get; }

    public GraphRepositoryTestFixture()
    {
        var mapper = MappingHelper.GetMapper(new MqttEdgeMappingProfile());
        GraphRepository = new EfGraphRepository(ContextFactory, mapper, new MqttEdgeMappingCache(), new MqttNodeCache(), _semaphoreProvider);
    }

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
        NodeGenerator.GenerateNode(nodeName: _mqttNodes[0].NodeId, nodeType: AgvGraph.NodeType.WayPoint, x: _mqttNodes[0].X, y: _mqttNodes[0].Y, z: (float) _mqttNodes[0].Z!),
        NodeGenerator.GenerateNode(nodeName: _mqttNodes[1].NodeId, nodeType: AgvGraph.NodeType.WayPoint, x: _mqttNodes[1].X, y: _mqttNodes[1].Y, z: (float) _mqttNodes[1].Z!),
        NodeGenerator.GenerateNode(nodeName: _mqttNodes[2].NodeId, nodeType: AgvGraph.NodeType.WayPoint, x: _mqttNodes[2].X, y: _mqttNodes[2].Y, z: (float) _mqttNodes[2].Z!),
        NodeGenerator.GenerateNode(nodeName: _mqttNodes[3].NodeId, nodeType: AgvGraph.NodeType.WayPoint, x: _mqttNodes[3].X, y: _mqttNodes[3].Y, z: (float) _mqttNodes[3].Z!),
        NodeGenerator.GenerateNode(nodeName: _mqttNodes[4].NodeId, nodeType: AgvGraph.NodeType.WayPoint, x: _mqttNodes[4].X, y: _mqttNodes[4].Y, z: (float) _mqttNodes[4].Z!)
    ];

    private static readonly GraphEdge[] _graphEdges =
    [
        GraphEdge.CreateDirect(_mqttNodes[0].NodeId.ToGuid(), _mqttNodes[1].NodeId.ToGuid(), 2f, Guid.Parse("ed70292e-e001-e002-8112-ffe69119c4b0")),
        GraphEdge.CreateDirect(_mqttNodes[1].NodeId.ToGuid(), _mqttNodes[2].NodeId.ToGuid(), 2f, Guid.Parse("ed70292e-e002-e003-8112-ffe69119c4b0")),
        GraphEdge.CreateDirect(_mqttNodes[2].NodeId.ToGuid(), _mqttNodes[3].NodeId.ToGuid(), 4f, Guid.Parse("ed70292e-e003-e004-8112-ffe69119c4b0")),
        GraphEdge.CreateDirect(_mqttNodes[3].NodeId.ToGuid(), _mqttNodes[4].NodeId.ToGuid(), 2f, Guid.Parse("ed70292e-e004-e005-8112-ffe69119c4b0")),
        GraphEdge.CreateDirect(_mqttNodes[4].NodeId.ToGuid(), _mqttNodes[3].NodeId.ToGuid(), 2f, Guid.Parse("ed70292e-e005-e004-8112-ffe69119c4b0"))
    ];

    public Guid GetGraphEdgeId(int index)
    {
        if (index >= 0 && index < _graphEdges.Length)
        {
            return _graphEdges[index].Id;
        }
        return Guid.Empty;
    }

    public async Task FillGraphRepository()
    {
        for (int i = 0; i < _mqttNodes.Length; i++)
        {
            await GraphRepository.Add(_graphNodes[i].Name, _mqttNodes[i]);
        }

        int j = 0;
        var edges = new List<(Guid, MqttEdge)>();
        for (int i = 0; i < _graphEdges.Length; i++)
        {
            edges.Add((_graphEdges[i].Id, _mqttEdges[j]));
            if (_mqttEdges[j].Bidirectional)
            {
                edges.Add((_graphEdges[++i].Id, _mqttEdges[j]));
            }
            j++;
        }

        await GraphRepository.Add(edges.ToArray());
    }
}