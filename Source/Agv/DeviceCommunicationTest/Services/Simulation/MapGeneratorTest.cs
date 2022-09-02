using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Map;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation
{

    /// <summary>
    /// Grid Generator Test
    /// </summary>
    [TestClass]
    public class MapGeneratorTest
    {
        /// <summary>
        /// Generate map
        /// </summary>
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
                new[]
                {
                    new AgvGraph.Node("S1") { Enabled = true, Type = AgvGraph.NodeType.Station, X =  0f, Y = -2f, Z = 0f },
                    new AgvGraph.Node("D1") { Enabled = true, Type = AgvGraph.NodeType.Station, X =  4f, Y =  6f, Z = 0f },
                    new AgvGraph.Node("S2") { Enabled = true, Type = AgvGraph.NodeType.Station, X =  7f, Y =  4f, Z = 0f },
                    new AgvGraph.Node("D2") { Enabled = true, Type = AgvGraph.NodeType.Station, X = -3f, Y =  0f, Z = 0f }
                },
                new[]
                {
                   new AgvGraph.Edge("S1", "D1") { Metric = 8.944f },   // Just take the distance
                   new AgvGraph.Edge("S2", "D2") { Metric = 10.77f }    // as metric.
                },
                mapName.ToGuid(),
                mapName);
            var map = new MapGenerator(graph).Generate();
            Assert.IsTrue(map.Bytes != null);
            Assert.IsTrue(map.Bytes.Length > 0);
            Assert.IsTrue(map.Resolution > 0f);
            Assert.IsTrue(map.XOffset > 0f);
            Assert.IsTrue(map.YOffset > 0f);
            var image = map.GetPng();
            Assert.IsTrue(image != null);
            Assert.IsTrue(image.Width > 0);
            Assert.IsTrue(image.Height > 0);
        }

        /// <summary>
        /// Generate map
        /// </summary>
        [TestMethod]
        public void GenerateMap_OriginOutside_OffsetNegatice()
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
                    new AgvGraph.Node("S1") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 1000f, Y = 1000f, Z = 0f },
                    new AgvGraph.Node("D1") { Enabled = true, Type = AgvGraph.NodeType.Station, X = 1010f, Y = 1010f, Z = 0f }
                },
                new[]
                {
                   new AgvGraph.Edge("S1", "D1") { Metric = 14f }
                },
                mapName.ToGuid(),
                mapName);
            var map = new MapGenerator(graph).Generate();
            Assert.IsTrue(map.Bytes != null);
            Assert.IsTrue(map.Bytes.Length > 0);
            Assert.IsTrue(map.Resolution > 0f);
            Assert.IsTrue(map.XOffset < 0f);
            Assert.IsTrue(map.YOffset < 0f);
        }

        /// <summary>
        /// Generate map
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GenerateMap_EmptyGraph_Failure()
        {
            var graph = new AgvGraph(Guid.Empty, Array.Empty<AgvGraph.Node>(), Array.Empty<AgvGraph.Edge>(),  Guid.Empty, "");
            var map = new MapGenerator(graph);
            map.Generate();
        }
    }
}
