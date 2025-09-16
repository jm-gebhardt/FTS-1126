using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Maps;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class GeneratedMapGeneratorTest
{
    [TestMethod]
    public void GenerateMap_CreateDataAndDecode_AllSuccessful()
    {
        /*  --S1------    x = origin (0/0)
         * |          |   Sx/Dx = node
         * D2 x       |
         * |          |
         * |          |
         * |          |
         * |          |
         * |         D1
         * |          |
         *  ------S2--    */

        var graph = new AgvGraph(Guid.Empty,
            new GraphNode[]
            {
                new("S1".ToGuid(), "S1") { Type = AgvGraph.NodeType.Pick, X = 0f, Y = -2f, Z = 0f },
                new("D1".ToGuid(), "D1") { Type = AgvGraph.NodeType.Drop, X = 4f, Y = 6f, Z = 0f },
                new("S2".ToGuid(), "S2") { Type = AgvGraph.NodeType.Pick, X = 7f, Y = 4f, Z = 0f },
                new("D2".ToGuid(), "D2") { Type = AgvGraph.NodeType.Drop, X = -3f, Y = 0f, Z = 0f }
            },
            new[]
            {
                GraphEdge.CreateDirect("S1".ToGuid(), "D1".ToGuid(), 8.944f), // Just take the distance
                GraphEdge.CreateDirect("S2".ToGuid(), "D2".ToGuid(), 10.77f) // as metric.
            });
        var map = new GeneratedMapGenerator().Generate(Guid.NewGuid(), "name", graph);
        map.ImageData.ShouldNotBeNull();
        map.ImageData.Length.ShouldBeGreaterThan(0);
        map.Resolution.ShouldBeGreaterThan(0f);
        map.XOffset.ShouldBeGreaterThan(0f);
        map.YOffset.ShouldBeGreaterThan(0f);
        Convert.FromBase64String(map.ImageData).Length.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void GenerateMap_OriginOutside_OffsetNegative()
    {
        /*     x                     x = origin (0/0)
         *             ----------    Sx/Dx = node
         *            |          |
         *            |  S1      |
         *            |          |
         *            |      D1  |
         *            |          |
         *             ----------    */

        var graph = new AgvGraph(Guid.Empty,
            [
                NodeGenerator.GenerateNode(AgvGraph.NodeType.Pick, 1000f, 1000f, nodeName: "S1"),
                NodeGenerator.GenerateNode(AgvGraph.NodeType.Drop, 1010f, 1010f, nodeName: "D1")
            ],
            [
                GraphEdge.CreateDirect("S1".ToGuid(), "D1".ToGuid(), 14f)
            ]);
        var map = new GeneratedMapGenerator().Generate(Guid.NewGuid(), "name", graph);
        map.ImageData.ShouldNotBeNull();
        map.ImageData.Length.ShouldBeGreaterThan(0);
        map.Resolution.ShouldBeGreaterThan(0f);
        map.XOffset.ShouldBeLessThan(0f);
        map.YOffset.ShouldBeLessThan(0f);
    }

    [TestMethod]
    public void GenerateMap_EmptyGraph_Failure()
    {
        var graph = new AgvGraph(Guid.Empty, Array.Empty<GraphNode>(), Array.Empty<GraphEdge>());
        Should.Throw<InvalidOperationException>(() => new GeneratedMapGenerator().Generate(Guid.Empty, "", graph));
    }
}