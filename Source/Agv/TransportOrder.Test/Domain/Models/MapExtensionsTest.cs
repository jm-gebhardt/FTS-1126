using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Testing.Time;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class MapExtensionsTest
{
    private readonly FakeTimeProvider _timeProvider = new();

    private static Map CreateMap(float resolution, int width, int height, float xOffset, float yOffset)
    {
        string image = MapHelper.GenerateWhitePngBase64(width, height);
        return new Map(Guid.NewGuid(), "test-map", DateTime.UtcNow, image, width, height, resolution, xOffset, yOffset);
    }

    [TestMethod]
    public void Extend_PreservesCorrelationIdAndMapIdAndResolution()
    {
        var map = CreateMap(resolution: 0.5f, width: 10, height: 10, xOffset: 0, yOffset: 0);
        var graph = new AgvGraph(Guid.NewGuid(), [new GraphNode { X = 5, Y = 5 }], []);

        var extended = map.Extend(graph, _timeProvider);

        extended.CorrelationId.ShouldBe(map.CorrelationId);
        extended.MapId.ShouldBe(map.MapId);
        extended.Resolution.ShouldBe(map.Resolution);
    }

    [TestMethod]
    public void Extend_SetsModificationDateFromTimeProvider()
    {
        var expectedTime = new DateTimeOffset(2024, 3, 10, 8, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(expectedTime);
        var map = CreateMap(resolution: 1f, width: 10, height: 10, xOffset: 0, yOffset: 0);
        var graph = new AgvGraph(Guid.NewGuid(), [new GraphNode { X = 5, Y = 5 }], []);

        var extended = map.Extend(graph, timeProvider);

        extended.ModificationDate.ShouldBe(expectedTime.UtcDateTime);
    }

    [TestMethod]
    public void Extend_GraphNodesAssignedToResultMap()
    {
        var map = CreateMap(resolution: 1f, width: 10, height: 10, xOffset: 0, yOffset: 0);
        var nodeId = Guid.NewGuid();
        var graphNode = new GraphNode(nodeId, "n1") { X = 5, Y = 5 };
        var graph = new AgvGraph(Guid.NewGuid(), [graphNode], []);

        var extended = map.Extend(graph, _timeProvider);

        extended.Nodes.Count.ShouldBe(1);
        extended.Nodes.Single().Id.ShouldBe(nodeId);
    }

    [TestMethod]
    public void Extend_WithOutputResolution_UsesOverrideForResolutionAndDimensions()
    {
        // Map: resolution=1, width=10, height=10, xOffset=5 → world [−5, 5]
        // Extra bounds: [−5, 95] × [−5, 95]
        // After union: minX=−5, maxX=95, rangeX=100, marginX=0, totalX=100
        // outputResolution=2: newWidth = ceil(100 / 2) = 50  
        var map = CreateMap(resolution: 1f, width: 10, height: 10, xOffset: 5f, yOffset: 5f);
        var graph = new AgvGraph(Guid.NewGuid(), [], []);

        var extended = map.Extend(graph, _timeProvider,
            extraBounds: (-5f, -5f, 95f, 95f),
            outputResolution: 2f);

        extended.Resolution.ShouldBe(2f);
        extended.Width.ShouldBe(50);
        extended.Height.ShouldBe(50);
    }
}
