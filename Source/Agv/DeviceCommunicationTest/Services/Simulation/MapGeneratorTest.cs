using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

[TestClass]
public class MapGeneratorTest
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

        const string mapName = "Map#1";
        var graph = new AgvGraph(Guid.Empty,
            new GraphNode[]
            {
                new("S1") { Type = AgvGraph.NodeType.Pick, X = 0f, Y = -2f, Z = 0f },
                new("D1") { Type = AgvGraph.NodeType.Drop, X = 4f, Y = 6f, Z = 0f },
                new("S2") { Type = AgvGraph.NodeType.Pick, X = 7f, Y = 4f, Z = 0f },
                new("D2") { Type = AgvGraph.NodeType.Drop, X = -3f, Y = 0f, Z = 0f }
            },
            new GraphEdge[]
            {
                new("S1", "D1", 8.944f), // Just take the distance
                new("S2", "D2", 10.77f) // as metric.
            },
            mapName.ToGuid(),
            mapName);
        var map = new MapGenerator(graph).Generate();
        map.ImageData.ShouldNotBeNull();
        map.ImageData.Length.ShouldBeGreaterThan(0);
        map.Resolution.ShouldBeGreaterThan(0f);
        map.XOffset.ShouldBeGreaterThan(0f);
        map.YOffset.ShouldBeGreaterThan(0f);
        var image = MapGenerator.GetPng(map);
        image.ShouldNotBeNull();
        image.Width.ShouldBeGreaterThan(0);
        image.Height.ShouldBeGreaterThan(0);
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

        const string mapName = "Map#1";
        var graph = new AgvGraph(Guid.Empty,
            new[]
            {
                new GraphNode("S1") { Type = AgvGraph.NodeType.Pick, X = 1000f, Y = 1000f, Z = 0f },
                new GraphNode("D1") { Type = AgvGraph.NodeType.Drop, X = 1010f, Y = 1010f, Z = 0f }
            },
            new[]
            {
                new GraphEdge("S1", "D1", 14f)
            },
            mapName.ToGuid(),
            mapName);
        var map = new MapGenerator(graph).Generate();
        map.ImageData.ShouldNotBeNull();
        map.ImageData.Length.ShouldBeGreaterThan(0);
        map.Resolution.ShouldBeGreaterThan(0f);
        map.XOffset.ShouldBeLessThan(0f);
        map.YOffset.ShouldBeLessThan(0f);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void GenerateMap_EmptyGraph_Failure()
    {
        var graph = new AgvGraph(Guid.Empty, Array.Empty<GraphNode>(), Array.Empty<GraphEdge>(), Guid.Empty, "");
        var map = new MapGenerator(graph);
        map.Generate();
    }
}