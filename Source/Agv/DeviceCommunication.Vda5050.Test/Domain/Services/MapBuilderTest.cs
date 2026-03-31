using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Time;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using Shouldly;
using DomainAgvGraph = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs.AgvGraph;
using CommonAgvGraph = Gebhardt.Acc.Common.Domain.Models.Graphs.AgvGraph;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class MapBuilderTest
{
    private readonly FakeTimeProvider _timeProvider = new();

    [TestMethod]
    public void BuildDummyMaps_SingleMapId_ReturnsOneMap()
    {
        var agvId = Guid.NewGuid();
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();

        var domainGraph = new DomainAgvGraph(agvId,
            [
                new Node { Id = nodeId1, NodeId = "n1", MapId = "map-A", Position = new NodePosition(0, 0), AgvActions = [] },
                new Node { Id = nodeId2, NodeId = "n2", MapId = "map-A", Position = new NodePosition(1, 1), AgvActions = [] }
            ],
            []);
        var commonGraph = new CommonAgvGraph(agvId,
            [
                new GraphNode(nodeId1, "n1") { X = 0, Y = 0 },
                new GraphNode(nodeId2, "n2") { X = 1, Y = 1 }
            ],
            []);

        List<Map> maps = MapBuilder.BuildDummyMaps(domainGraph, commonGraph, _timeProvider).ToList();

        maps.Count.ShouldBe(1);
        maps.Single().MapId.ShouldBe("map-A");
        maps.Single().Nodes.Count.ShouldBe(2);
    }

    [TestMethod]
    public void BuildDummyMaps_TwoMapIds_ReturnsTwoMaps()
    {
        var agvId = Guid.NewGuid();
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();

        var domainGraph = new DomainAgvGraph(agvId,
            [
                new Node { Id = nodeId1, NodeId = "n1", MapId = "map-A", Position = new NodePosition(0, 0), AgvActions = [] },
                new Node { Id = nodeId2, NodeId = "n2", MapId = "map-B", Position = new NodePosition(1, 1), AgvActions = [] }
            ],
            []);
        var commonGraph = new CommonAgvGraph(agvId,
            [
                new GraphNode(nodeId1, "n1") { X = 0, Y = 0 },
                new GraphNode(nodeId2, "n2") { X = 1, Y = 1 }
            ],
            []);

        List<Map> maps = MapBuilder.BuildDummyMaps(domainGraph, commonGraph, _timeProvider).ToList();

        maps.Count.ShouldBe(2);
        maps.ShouldContain(m => m.MapId == "map-A");
        maps.ShouldContain(m => m.MapId == "map-B");
    }

    [TestMethod]
    public void BuildDummyMaps_MapContainsOnlyItsOwnNodes()
    {
        var agvId = Guid.NewGuid();
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();

        var domainGraph = new DomainAgvGraph(agvId,
            [
                new Node { Id = nodeId1, NodeId = "n1", MapId = "map-A", Position = new NodePosition(0, 0), AgvActions = [] },
                new Node { Id = nodeId2, NodeId = "n2", MapId = "map-B", Position = new NodePosition(1, 1), AgvActions = [] }
            ],
            []);
        var commonGraph = new CommonAgvGraph(agvId,
            [
                new GraphNode(nodeId1, "n1") { X = 0, Y = 0 },
                new GraphNode(nodeId2, "n2") { X = 1, Y = 1 }
            ],
            []);

        List<Map> maps = MapBuilder.BuildDummyMaps(domainGraph, commonGraph, _timeProvider).ToList();

        var mapA = maps.Single(m => m.MapId == "map-A");
        mapA.Nodes.Count.ShouldBe(1);
        mapA.Nodes.Single().Id.ShouldBe(nodeId1);
    }

    [TestMethod]
    public void BuildDummyMaps_EdgesAssignedToCorrectMap()
    {
        var agvId = Guid.NewGuid();
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();
        var nodeId3 = Guid.NewGuid();
        var edgeId1 = Guid.NewGuid();
        var edgeId2 = Guid.NewGuid();

        var domainGraph = new DomainAgvGraph(agvId,
            [
                new Node { Id = nodeId1, NodeId = "n1", MapId = "map-A", Position = new NodePosition(0, 0), AgvActions = [] },
                new Node { Id = nodeId2, NodeId = "n2", MapId = "map-A", Position = new NodePosition(1, 1), AgvActions = [] },
                new Node { Id = nodeId3, NodeId = "n3", MapId = "map-B", Position = new NodePosition(2, 2), AgvActions = [] }
            ],
            [
                new Edge { Id = edgeId1, EdgeId = "e1", StartNodeId = "n1", EndNodeId = "n2", AgvTrajectory = new Trajectory(), AgvActions = [] },
                new Edge { Id = edgeId2, EdgeId = "e2", StartNodeId = "n1", EndNodeId = "n3", AgvTrajectory = new Trajectory(), AgvActions = [] }
            ]);
        var commonGraph = new CommonAgvGraph(agvId,
            [
                new GraphNode(nodeId1, "n1") { X = 0, Y = 0 },
                new GraphNode(nodeId2, "n2") { X = 1, Y = 1 },
                new GraphNode(nodeId3, "n3") { X = 2, Y = 2 }
            ],
            [
                new GraphEdge { Id = edgeId1, FromNodeId = nodeId1, ToNodeId = nodeId2 },
                new GraphEdge { Id = edgeId2, FromNodeId = nodeId1, ToNodeId = nodeId3 }
            ]);

        List<Map> maps = MapBuilder.BuildDummyMaps(domainGraph, commonGraph, _timeProvider).ToList();

        var mapA = maps.Single(m => m.MapId == "map-A");
        mapA.Edges.Count.ShouldBe(1);
        mapA.Edges.Single().Id.ShouldBe(edgeId1);
    }

    [TestMethod]
    public void BuildDummyMaps_SetsImageData()
    {
        var agvId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();

        var domainGraph = new DomainAgvGraph(agvId,
            [new Node { Id = nodeId, NodeId = "n1", MapId = "map", Position = new NodePosition(0, 0), AgvActions = [] }],
            []);
        var commonGraph = new CommonAgvGraph(agvId,
            [new GraphNode(nodeId, "n1") { X = 0, Y = 0 }],
            []);

        List<Map> maps = MapBuilder.BuildDummyMaps(domainGraph, commonGraph, _timeProvider).ToList();

        maps.Single().ImageData.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void BuildDummyMaps_DimensionsMatchNodeSpread()
    {
        var agvId = Guid.NewGuid();
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();

        var domainGraph = new DomainAgvGraph(agvId,
            [
                new Node { Id = nodeId1, NodeId = "n1", MapId = "map", Position = new NodePosition(0, 0), AgvActions = [] },
                new Node { Id = nodeId2, NodeId = "n2", MapId = "map", Position = new NodePosition(100, 50), AgvActions = [] }
            ],
            []);
        var commonGraph = new CommonAgvGraph(agvId,
            [
                new GraphNode(nodeId1, "n1") { X = 0, Y = 0 },
                new GraphNode(nodeId2, "n2") { X = 100, Y = 50 }
            ],
            []);

        List<Map> maps = MapBuilder.BuildDummyMaps(domainGraph, commonGraph, _timeProvider).ToList();

        var map = maps.Single();
        map.Width.ShouldBeGreaterThan(0);
        map.Height.ShouldBeGreaterThan(0);
        map.Resolution.ShouldBeGreaterThan(0);
        map.Width.ShouldBeGreaterThan(map.Height);
    }
}