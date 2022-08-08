using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        /// Test recognition of missing input att all.
        /// </summary>
        [TestMethod]
        public void Check_WithInvalidInput_NoExceptionWhileCheckingValidity()
        {
            var parser = new StationListParser(
                new MqttStationList("", DateTimeOffset.UtcNow, Array.Empty<Station>()),
                new MqttGraph("", DateTimeOffset.UtcNow, Array.Empty<Node>(), Array.Empty<Edge>(), string.Empty));

            Assert.IsFalse(parser.SerialNumberValid);
            Assert.IsFalse(parser.StationListValid);
        }

        /// <summary>
        /// Test recognition of missing station list.
        /// </summary>
        [TestMethod]
        public void Check_WithMissingStationList_NoExceptionWhileCheckingValidity()
        {
            var parser = new StationListParser(
                new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<Station>()),
                new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<Node>(), Array.Empty<Edge>(), string.Empty));

            Assert.IsTrue(parser.SerialNumberValid);
            Assert.IsFalse(parser.StationListValid);
        }

        /// <summary>
        /// Test recognition of missing serial number.
        /// </summary>
        [TestMethod]
        public void Check_WithSerialNumberMismatch_NoExceptionWhileCheckingValidity()
        {
            var stations = new Station[] {
                new(Function.Pick, "P01", new Position(0f, 0f, 0f), "")
            };
            var parser = new StationListParser(
                new MqttStationList(AgvTestIdentifier02, DateTimeOffset.UtcNow, stations),
                new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<Node>(), Array.Empty<Edge>(), string.Empty));

            Assert.IsFalse(parser.SerialNumberValid);
            Assert.IsTrue(parser.StationListValid);
        }

        /// <summary>
        /// Test for exception when parsing without graph information.
        /// </summary>
        [TestMethod]
        public void Parse_WithValidStationDataAndMissingGraph_CheckOkParseException()
        {
            var stations = new Station[] {
                new(Function.Pick, "P01", new Position(0f, 0f, 0f), "")
            };
            var parser = new StationListParser(
                new MqttStationList(AgvTestIdentifier01, DateTimeOffset.UtcNow, stations),
                new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<Node>(), Array.Empty<Edge>(), string.Empty));

            Assert.IsTrue(parser.SerialNumberValid);
            Assert.IsTrue(parser.StationListValid);
            Assert.ThrowsException<InvalidOperationException>(() => parser.Graph);
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

            Assert.IsTrue(parser.SerialNumberValid);
            Assert.IsTrue(parser.StationListValid);

            var agvGraph = parser.Graph;

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
