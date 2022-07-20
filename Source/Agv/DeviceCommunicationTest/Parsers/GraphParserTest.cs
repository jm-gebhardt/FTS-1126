using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MessageParsers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Parsers
{
    /// <summary>
    /// Tests for <see cref="GraphParser"/>.
    /// </summary>
    [TestClass]
    public class GraphParserTest
    {
        private const string AgvTestIdentifier01 = "#2410#";
        private const string AgvTestIdentifier02 = "#0815#";

        /// <summary>
        /// Test for parsing and checking incomplete  AGV graph message.
        /// </summary>
        [TestMethod]
        public void ParseGraphMessage_WithInvalidInput_NoExceptionWhileCheckingValidity()
        {
            var parser = new GraphParser(new MqttGraph("", DateTimeOffset.UtcNow, 0, 0, Array.Empty<Node>(), Array.Empty<Edge>()));

            Assert.IsFalse(parser.SerialNumberValid);
            Assert.IsFalse(parser.NodesValid);
            Assert.IsFalse(parser.EdgesValid);
        }

        /// <summary>
        /// Test for parsing invalid AGV graph message.
        /// </summary>
        [TestMethod]
        public void ParseGraphMessage_WithEmptySerialNumber_Exception()
        {
            var parser = new GraphParser(new MqttGraph("", DateTimeOffset.UtcNow, 0, 0, Array.Empty<Node>(), Array.Empty<Edge>()));

            Assert.IsFalse(parser.SerialNumberValid);
            Assert.IsFalse(parser.NodesValid);
            Assert.IsFalse(parser.EdgesValid);
            Assert.ThrowsException<InvalidOperationException>(() => parser.SerialNumber);
        }

        /// <summary>
        /// Test for parsing invalid AGV graph message.
        /// </summary>
        [TestMethod]
        public void ParseGraphMessage_WithNodeMismatch1_Exception()
        {
            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f }
            };

            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, 0, 0, nodes, Array.Empty<Edge>()));

            Assert.IsTrue(parser.SerialNumberValid);
            Assert.IsFalse(parser.NodesValid);
            Assert.IsFalse(parser.EdgesValid);
            Assert.ThrowsException<InvalidOperationException>(() => parser.Nodes);
        }

        /// <summary>
        /// Test for parsing invalid AGV graph message.
        /// </summary>
        [TestMethod]
        public void ParseGraphMessage_WithNodeMismatch2_Exception()
        {
            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 0f, Y = 0f, Z = 0f }
            };

            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, 1, 0, nodes, Array.Empty<Edge>()));

            Assert.IsTrue(parser.SerialNumberValid);
            Assert.IsFalse(parser.NodesValid);
            Assert.IsFalse(parser.EdgesValid);
            Assert.ThrowsException<InvalidOperationException>(() => parser.Nodes);
        }

        /// <summary>
        /// Test for parsing invalid AGV graph message.
        /// </summary>
        [TestMethod]
        public void ParseGraphMessage_WithEdgeMismatch1_Exception()
        {
            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 0f, Y = 0f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
            };
            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, 2, 0, nodes, edges));

            Assert.IsTrue(parser.SerialNumberValid);
            Assert.IsTrue(parser.NodesValid);
            Assert.IsFalse(parser.EdgesValid);
            Assert.ThrowsException<InvalidOperationException>(() => parser.Edges);
        }

        /// <summary>
        /// Test for parsing AGV graph message.
        /// </summary>
        [TestMethod]
        public void ParseGraphMessage_WithUnidirectionalEdge_ExpectOne2OneConversion()
        {
            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
            };
            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, 2, 1, nodes, edges));

            Assert.IsTrue(parser.SerialNumberValid);
            Assert.IsTrue(parser.NodesValid);
            Assert.IsTrue(parser.EdgesValid);

            Assert.AreEqual(AgvTestIdentifier02, parser.SerialNumber);
            Assert.AreEqual(2, parser.Nodes.Count);
            Assert.AreEqual(1, parser.Edges.Count);
        }

        /// <summary>
        /// Test for parsing AGV graph message.
        /// </summary>
        [TestMethod]
        public void ParseGraphMessage_WithBidirectionEdge_ExpectOne2TwoConversion()
        {
            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
            };

            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, 2, 1, nodes, edges));

            Assert.IsTrue(parser.SerialNumberValid);
            Assert.IsTrue(parser.NodesValid);
            Assert.IsTrue(parser.EdgesValid);

            Assert.AreEqual(AgvTestIdentifier02, parser.SerialNumber);

            Assert.AreEqual(2, parser.Nodes.Count);

            // Edge has to be dublicated because it is bidirectional.
            Assert.AreEqual(2, parser.Edges.Count);
        }

        /// <summary>
        /// Test for parsing AGV graph message.
        /// </summary>
        [TestMethod]
        public void ParseGraphMessage_WithAttributes_ExpectAdditionalEdges()
        {
            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 1f, Attributes = new[]
                    {
                        new DeviceCommunication.MqttMessages.Attribute { Key = Node.WellKnownAttributes.Station.ToString(), Value = "11" },
                        new DeviceCommunication.MqttMessages.Attribute { Key = Node.WellKnownAttributes.Station.ToString(), Value = "12" }
                    }
                },
                new() { NodeId = 2, X = 100f, Y = 100f, Z = 2f, Attributes = new[]
                    {
                        new DeviceCommunication.MqttMessages.Attribute { Key = Node.WellKnownAttributes.Station.ToString(), Value = "21" }
                    }
                }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
            };

            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, 2, 1, nodes, edges));

            Assert.IsTrue(parser.SerialNumberValid);
            Assert.IsTrue(parser.NodesValid);
            Assert.IsTrue(parser.EdgesValid);

            Assert.AreEqual(AgvTestIdentifier02, parser.SerialNumber);
            // 2 + 3 additional nodes by attributes.
            Assert.AreEqual(5, parser.Nodes.Count);
            Assert.AreEqual(1f, parser.Nodes.Single(x => x.Id == "1").Z, 0.00001);
            Assert.AreEqual(1f, parser.Nodes.Single(x => x.Id == "11").Z, 0.00001);
            Assert.AreEqual(1f, parser.Nodes.Single(x => x.Id == "12").Z, 0.00001);
            Assert.AreEqual(2f, parser.Nodes.Single(x => x.Id == "2").Z, 0.00001);
            Assert.AreEqual(2f, parser.Nodes.Single(x => x.Id == "21").Z, 0.00001);

            // 2 (bidirectional edge) + 2 (bidirectional edge) * 3 (internal nodes).
            Assert.AreEqual(8, parser.Edges.Count);
        }
    }
}
