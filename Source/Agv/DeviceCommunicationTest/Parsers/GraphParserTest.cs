using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
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
        /// Test for parsing invalid AGV graph message with serial number missing.
        /// </summary>
        [TestMethod]
        public void Parse_WithoutSerialNumber_GraphInvalid()
        {
            var parser = new GraphParser(new MqttGraph("", DateTimeOffset.UtcNow, Array.Empty<Node>(), Array.Empty<Edge>(), string.Empty));
            var graph = parser.Graph;

            Assert.AreEqual(AgvGraph.GraphUsability.SerialNumberMissing, graph.Usability);
        }

        /// <summary>
        /// Test for parsing invalid AGV graph message with missing nodes and edges.
        /// </summary>
        [TestMethod]
        public void Parse_WithEmptyNodes_GraphInvalid()
        {
            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<Node>(), Array.Empty<Edge>(), string.Empty));
            Assert.ThrowsException<InvalidOperationException>(() => parser.SerialNumber);

            var graph = parser.Graph;
            Assert.AreEqual(AgvGraph.GraphUsability.InvalidNodes, graph.Usability);
        }

        /// <summary>
        /// Test for parsing invalid AGV graph message with missing edges.
        /// </summary>
        [TestMethod]
        public void Parse_WithEmptyEdges_GraphInvalid()
        {
            var nodes = new Node[]
            {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f }
            };
            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, Array.Empty<Edge>(), string.Empty));
            Assert.ThrowsException<InvalidOperationException>(() => parser.SerialNumber);

            var graph = parser.Graph;
            Assert.AreEqual(AgvGraph.GraphUsability.InvalidEdges, graph.Usability);
        }

        /// <summary>
        /// Test for parsing valid AGV graph message.
        /// </summary>
        [TestMethod]
        public void Parse_ValidInput_GraphValid()
        {
            var nodes = new Node[]
            {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 0f, Y = 0f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
            };
            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
            var graph = parser.Graph;

            Assert.AreEqual(AgvTestIdentifier01, parser.SerialNumber);
            Assert.AreEqual(AgvGraph.GraphUsability.Valid, graph.Usability);
            Assert.AreEqual(2, graph.Nodes.Length);
            Assert.AreEqual(1, graph.Edges.Length);
        }

        /// <summary>
        /// Test for parsing valid AGV graph message with optional input
        /// </summary>
        [TestMethod]
        public void Parse_WithOptionalInput_GraphValid()
        {
            const string mapId = "MAP01";

            var nodes = new Node[]
            {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 0f, Y = 0f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
            };
            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, mapId, Guid.NewGuid()));
            var graph = parser.Graph;

            Assert.AreEqual(AgvTestIdentifier01, parser.SerialNumber);
            Assert.AreEqual(AgvGraph.GraphUsability.Valid, graph.Usability);
            Assert.AreEqual(mapId, graph.RelatedMapName);
            Assert.AreEqual(mapId.ToGuid(), graph.RelatedMapId);
            Assert.AreEqual(AgvTestIdentifier01.ToGuid(), graph.CorrelationId);
        }

        /// <summary>
        /// Test for parsing invalid AGV graph and try to get velocity information
        /// </summary>
        [TestMethod]
        public void Parse_InvalidGraph_ExceptionWhenGettingVelocityInformation()
        {
            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<Node>(), Array.Empty<Edge>(), string.Empty));

            Assert.ThrowsException<InvalidOperationException>(() => parser.ConsiderVelocity);
            Assert.ThrowsException<InvalidOperationException>(() => parser.AverageVelocity);

            var graph = parser.Graph;
            Assert.AreEqual(AgvGraph.GraphUsability.InvalidNodes, graph.Usability);
        }

        /// <summary>
        /// Test for parsing valid AGV graph message without velocity information.
        /// </summary>
        [TestMethod]
        public void Parse_WithoutVelocityButAccessVelocity_ExceptionButGraphValid()
        {
            var nodes = new Node[]
            {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 0f, Y = 0f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 0 }
            };
            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, string.Empty));

            Assert.ThrowsException<InvalidOperationException>(() => parser.AverageVelocity);

            var graph = parser.Graph;
            Assert.AreEqual(AgvTestIdentifier01, parser.SerialNumber);
            Assert.AreEqual(AgvGraph.GraphUsability.Valid, graph.Usability);
        }

        /// <summary>
        /// Test for parsing AGV graph message.
        /// </summary>
        [TestMethod]
        public void Parse_WithUnidirectionalEdge_ExpectOne2OneConversion()
        {
            var nodes = new Node[]
            {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
            };
            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));

            var graph = parser.Graph;
            Assert.AreEqual(AgvTestIdentifier02, parser.SerialNumber);
            Assert.AreEqual(AgvGraph.GraphUsability.Valid, graph.Usability);
            Assert.AreEqual(2, graph.Nodes.Length);
            Assert.AreEqual(1, graph.Edges.Length);
        }

        /// <summary>
        /// Test for parsing AGV graph message.
        /// </summary>
        [TestMethod]
        public void Parse_WithBidirectionalEdge_ExpectOne2TwoConversion()
        {
            var nodes = new Node[]
            {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
            };

            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow,  nodes, edges, string.Empty));

            var graph = parser.Graph;
            Assert.AreEqual(AgvTestIdentifier02, parser.SerialNumber);
            Assert.AreEqual(AgvGraph.GraphUsability.Valid, graph.Usability);
            Assert.AreEqual(2, graph.Nodes.Length);
            // Edge has to be duplicated because it is bidirectional.
            Assert.AreEqual(2, graph.Edges.Length);
        }

        /// <summary>
        /// Test for parsing AGV graph message.
        /// </summary>
        [TestMethod]
        public void Parse_WithVelocity_ExpectNoAdditionalEdges()
        {
            var nodes = new Node[]
            {
                // Square with 4 edges.
                new() { NodeId = 1, X =  0f, Y =  0f, Z = 0f },
                new() { NodeId = 2, X = 10f, Y =  0f, Z = 0f },
                new() { NodeId = 3, X = 10f, Y = 10f, Z = 0f },
                new() { NodeId = 4, X =  0f, Y = 10f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 10 },
                new() { EdgeId = 2, StartId = 2, EndId = 3, Bidirectional = false, MaxVelocity = 20 },
                new() { EdgeId = 3, StartId = 3, EndId = 4, Bidirectional = false, MaxVelocity = 40 },
                new() { EdgeId = 4, StartId = 4, EndId = 1, Bidirectional = false, MaxVelocity = 10 }
            };

            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));

            var graph = parser.Graph;
            Assert.AreEqual(AgvTestIdentifier02, parser.SerialNumber);
            Assert.AreEqual(AgvGraph.GraphUsability.Valid, graph.Usability);
            Assert.AreEqual(4, graph.Nodes.Length);
            Assert.AreEqual(4, graph.Edges.Length);
            //              4x distance / 4x durations
            Assert.AreEqual(4f * 10     / (10f/10 + 10f/20 + 10f/40 + 10f/10), parser.AverageVelocity, 0.00001);
        }

        /// <summary>
        /// Test for parsing AGV graph message with invalid nodes.
        /// </summary>
        [TestMethod]
        public void Parse_WithInvalidNodes_GraphInvalid()
        {
            var nodes = new Node[]
            {
                new() { NodeId = 1, X =  0f, Y =  0f, Z = 0f },
                // Ambiguous node id
                new() { NodeId = 1, X = 10f, Y =  0f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 1, Bidirectional = false, MaxVelocity = 10 }
            };

            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
            Assert.ThrowsException<InvalidOperationException>(() => parser.SerialNumber);

            var graph = parser.Graph;
            Assert.AreEqual(AgvGraph.GraphUsability.InvalidNodes, graph.Usability);
        }

        /// <summary>
        /// Test for parsing AGV graph message with invalid edges.
        /// </summary>
        [TestMethod]
        public void Parse_WithInvalidEdges_GraphInvalid()
        {
            var nodes = new Node[]
            {
                new() { NodeId = 1, X =  0f, Y =  0f, Z = 0f },
                new() { NodeId = 2, X = 10f, Y =  0f, Z = 0f }
            };
            Edge[] edges = Array.Empty<Edge>();

            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
            Assert.ThrowsException<InvalidOperationException>(() => parser.SerialNumber);

            var graph = parser.Graph;
            Assert.AreEqual(AgvGraph.GraphUsability.InvalidEdges, graph.Usability);
        }

        [TestMethod]
        public void Parse_WithDuplicateEdges_GraphInvalid()
        {
            var nodes = new Node[]
            {
                new() { NodeId = 1, X =  0f, Y =  0f, Z = 0f },
                new() { NodeId = 2, X = 10f, Y =  0f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 1, Bidirectional = false, MaxVelocity = 10 },
                // Duplicate edge
                new() { EdgeId = 1, StartId = 1, EndId = 1, Bidirectional = false, MaxVelocity = 10 }
            };

            var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
            Assert.ThrowsException<InvalidOperationException>(() => parser.SerialNumber);

            var graph = parser.Graph;
            Assert.AreEqual(AgvGraph.GraphUsability.InvalidEdges, graph.Usability);
        }
    }
}
