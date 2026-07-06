using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Testing.Time;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Services;

[TestClass]
public class MapBuilderTest
{
    private readonly FakeTimeProvider _timeProvider = new();

    [TestMethod]
    public void BuildDummyMap_ReturnsMapWithGivenMapId()
    {
        var graph = new AgvGraph(Guid.NewGuid(),
            [new GraphNode { X = 0, Y = 0 }, new GraphNode { X = 1, Y = 1 }],
            []);

        var map = MapBuilder.BuildDummyMap("map-A", graph, _timeProvider);

        map.MapId.ShouldBe("map-A");
    }

    [TestMethod]
    public void BuildDummyMap_ContainsAllGraphNodes()
    {
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();
        var graph = new AgvGraph(Guid.NewGuid(),
            [
                new GraphNode(nodeId1, "n1") { X = 0, Y = 0 },
                new GraphNode(nodeId2, "n2") { X = 1, Y = 1 }
            ],
            []);

        var map = MapBuilder.BuildDummyMap("map-A", graph, _timeProvider);

        map.Nodes.Count.ShouldBe(2);
    }

    [TestMethod]
    public void BuildDummyMap_SetsNonEmptyImageData()
    {
        var graph = new AgvGraph(Guid.NewGuid(),
            [new GraphNode { X = 0, Y = 0 }],
            []);

        var map = MapBuilder.BuildDummyMap("map", graph, _timeProvider);

        map.ImageData.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void BuildDummyMap_WidthGreaterThanHeight_ForHorizontalNodeSpread()
    {
        var graph = new AgvGraph(Guid.NewGuid(),
            [
                new GraphNode { X = 0, Y = 0 },
                new GraphNode { X = 100, Y = 10 }
            ],
            []);

        var map = MapBuilder.BuildDummyMap("map", graph, _timeProvider);

        map.Width.ShouldBeGreaterThan(0);
        map.Height.ShouldBeGreaterThan(0);
        map.Width.ShouldBeGreaterThan(map.Height);
    }

    [TestMethod]
    public void BuildDummyMap_UsesTimeProviderForModificationDate()
    {
        var expectedTime = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(expectedTime);
        var graph = new AgvGraph(Guid.NewGuid(), [new GraphNode { X = 0, Y = 0 }], []);

        var map = MapBuilder.BuildDummyMap("map", graph, timeProvider);

        map.ModificationDate.ShouldBe(expectedTime.UtcDateTime);
    }

    [TestMethod]
    public void BuildDummyMap_ContainsAllGraphEdges()
    {
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();
        var edgeId = Guid.NewGuid();
        var graph = new AgvGraph(Guid.NewGuid(),
            [
                new GraphNode(nodeId1, "n1") { X = 0, Y = 0 },
                new GraphNode(nodeId2, "n2") { X = 1, Y = 1 }
            ],
            [new GraphEdge { Id = edgeId, FromNodeId = nodeId1, ToNodeId = nodeId2 }]);

        var map = MapBuilder.BuildDummyMap("map", graph, _timeProvider);

        map.Edges.Count.ShouldBe(1);
    }
}
