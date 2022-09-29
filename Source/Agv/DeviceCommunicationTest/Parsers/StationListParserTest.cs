using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MessageParsers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Parsers
{
    /// <summary>
    /// Tests for <see cref="StationListParser"/>.
    /// </summary>
    [TestClass]
    public class StationListParserTest
    {
        private const string AgvTestIdentifier01 = "#2410#";
        private const string AgvTestIdentifier02 = "#0815#";

        /// <summary>
        /// Test recognition of missing input at all.
        /// </summary>
        [TestMethod]
        public void Parse_WithInvalidInputAtAll_GraphInvalid()
        {
            var parser = new StationListParser(
                new MqttStationList("", DateTimeOffset.UtcNow, Array.Empty<Station>()),
                new MqttGraph("", DateTimeOffset.UtcNow, Array.Empty<Node>(), Array.Empty<Edge>(), string.Empty));
            var graph = parser.Graph;

            Assert.AreEqual(Common.Messages.AgvManagement.AgvGraph.GraphUsability.SerialNumberMissing, graph.Usability);
        }

        /// <summary>
        /// Test recognition of missing nodes.
        /// </summary>
        [TestMethod]
        public void Parse_WithMissingGraphEdgeAndStation_GraphInvalid()
        {
            var parser = new StationListParser(
                new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<Station>()),
                new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<Node>(), Array.Empty<Edge>(), string.Empty));
            var graph = parser.Graph;

            Assert.AreEqual(Common.Messages.AgvManagement.AgvGraph.GraphUsability.InvalidNodes, graph.Usability);
            Assert.ThrowsException<InvalidOperationException>(() => parser.SerialNumber);
        }

        /// <summary>
        /// Test recognition of missing edges
        /// </summary>
        [TestMethod]
        public void Parse_WithMissingEdgeAndStation_NoExceptionWhileCheckingValidity()
        {
            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 9f, Y = 9f, Z = 0f }
            };
            var parser = new StationListParser(
                new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<Station>()),
                new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, Array.Empty<Edge>(), string.Empty));
            var graph = parser.Graph;

            Assert.AreEqual(Common.Messages.AgvManagement.AgvGraph.GraphUsability.InvalidEdges, graph.Usability);
        }

        /// <summary>
        /// Test recognition of missing staions
        /// </summary>
        [TestMethod]
        public void Parse_WithMissingStation__GraphInvalid()
        {
            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 9f, Y = 9f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
            };
            var parser = new StationListParser(
                new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<Station>()),
                new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
            var graph = parser.Graph;

            Assert.AreEqual(Common.Messages.AgvManagement.AgvGraph.GraphUsability.InvalidStations, graph.Usability);
        }

        /// <summary>
        /// Test minimal valid graph
        /// </summary>
        [TestMethod]
        public void Parse_WithMinimumContent_GraphValid()
        {
            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 9f, Y = 9f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
            };
            var stations = new Station[] {
                new(Function.Pick, "P01", new Position(0f, 0f, 0f), "")
            };
            var parser = new StationListParser(
                new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow, stations),
                new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
            var graph = parser.Graph;

            Assert.AreEqual(Common.Messages.AgvManagement.AgvGraph.GraphUsability.Valid, graph.Usability);
            Assert.AreEqual(AgvTestIdentifier01, parser.SerialNumber);
            //              3 = 2 nodes + 1 station
            Assert.AreEqual(3, graph.Nodes.Length);
            //              4 = 2 * 2 bidirectional edges
            Assert.AreEqual(4, graph.Edges.Length);
        }

        /// <summary>
        /// Test valid graph with additional optional content
        /// </summary>
        [TestMethod]
        public void Parse_WithOptionalContent_GraphValid()
        {
            const string mapId = "MAP01";

            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 9f, Y = 9f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
            };
            var stations = new Station[] {
                new(Function.Pick, "P01", new Position(0f, 0f, 0f), "")
            };
            var parser = new StationListParser(
                new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow, stations),
                new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, mapId, Guid.NewGuid()));
            var graph = parser.Graph;

            Assert.AreEqual(Common.Messages.AgvManagement.AgvGraph.GraphUsability.Valid, graph.Usability);
            Assert.AreEqual(AgvTestIdentifier01, parser.SerialNumber);
            Assert.AreEqual(mapId, graph.RelatedMapName);
            Assert.AreEqual(mapId.ToGuid(), graph.RelatedMapId);
            Assert.AreEqual(AgvTestIdentifier01.ToGuid(), graph.CorrelationId);
        }

        /// <summary>
        /// Test minimal valid graph
        /// </summary>
        [TestMethod]
        public void Parse_WithSerialNumberMismatch_GraphInvalid()
        {
            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 9f, Y = 9f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
            };
            var stations = new Station[] {
                new(Function.Pick, "P01", new Position(0f, 0f, 0f), "")
            };
            var parser = new StationListParser(
                new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow, stations),
                new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
            var graph = parser.Graph;

            Assert.AreEqual(Common.Messages.AgvManagement.AgvGraph.GraphUsability.SerialNumberMismatch, graph.Usability);
        }

        /// <summary>
        /// Test parsing with valid data and check result of merging.
        /// </summary>
        [TestMethod]
        public void Parse_WithValidData_AllOk()
        {
            /*    0123456789
             *    ----------
             * -2|  S1  S3  |
             * -1| /   /    |
             *  0|1---      |
             *  1| \        |
             *  2|  \       |
             *  3|   \      |
             *  4|    \     |
             *  5|     \    |
             *  6|      \   |
             *  7|       \  |
             *  8|        \ |
             *  9|         2|
             * 10|        / |
             * 11|       S2 |
             *    ---------- */

            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 9f, Y = 9f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
            };
            var stations = new Station[] {
                new(Function.Both, "S1", new Position(0f, 2f, -2f), ""),
                new(Function.Both, "S2", new Position(0f, 7f, 11f), ""),
                new(Function.Both, "S3", new Position(0f, 6f, -2f), "")
            };
            var parser = new StationListParser(
                new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow, stations),
                new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, string.Empty));

            var agvGraph = parser.Graph;
            Assert.AreEqual(AgvTestIdentifier01, parser.SerialNumber);

            Assert.AreEqual(5, agvGraph.Nodes.Length);
            Assert.AreEqual(8, agvGraph.Edges.Length);

            // Average velocity for the single edge is 1 (metric[t] = SQRT(9^2+9^2)/1 = 12.72792.

            // Distance S1->1 is 2 (metric[t] = SQRT(2^2+2^2)/1 = 2,82843
            float metric = (float)Math.Sqrt(8);
            Assert.AreEqual(metric, agvGraph.Edges.First(x => x.From == "S1").Metric, 0.00001);
            Assert.AreEqual(metric, agvGraph.Edges.First(x => x.To == "S1").Metric, 0.00001);

            // Distance S2->2 is 2 (metric[t] = SQRT(2^2+2^2)/1 = 2,82843
            metric = (float)Math.Sqrt(8);
            Assert.AreEqual(metric, agvGraph.Edges.First(x => x.From == "S2").Metric, 0.00001);
            Assert.AreEqual(metric, agvGraph.Edges.First(x => x.To == "S2").Metric, 0.00001);

            // Distance S3->1 is 2 (metric[t] = SQRT(6^2+2^2)/1 = 6,32456
            metric = (float)Math.Sqrt(40);
            Assert.AreEqual(metric, agvGraph.Edges.First(x => x.From == "S3").Metric, 0.00001);
            Assert.AreEqual(metric, agvGraph.Edges.First(x => x.To == "S3").Metric, 0.00001);
        }
    }
}
