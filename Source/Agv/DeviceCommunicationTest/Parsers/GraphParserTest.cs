using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Exceptions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MessageParsers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Parsers;

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
        var parser = new GraphParser(new MqttGraph("", DateTimeOffset.UtcNow, Array.Empty<MqttNode>(), Array.Empty<MqttEdge>(), string.Empty));
        var graph = parser.Graph;

        graph.Usability.ShouldBe(AgvGraph.GraphUsability.SerialNumberMissing);
    }

    /// <summary>
    /// Test for parsing invalid AGV graph message with missing nodes and edges.
    /// </summary>
    [TestMethod]
    public void Parse_WithEmptyNodes_GraphInvalid()
    {
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<MqttNode>(), Array.Empty<MqttEdge>(), string.Empty));
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);

        var graph = parser.Graph;
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidNodes);
    }

    /// <summary>
    /// Test for parsing invalid AGV graph message with missing edges.
    /// </summary>
    [TestMethod]
    public void Parse_WithEmptyEdges_GraphInvalid()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f }
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, Array.Empty<MqttEdge>(), string.Empty));
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);

        var graph = parser.Graph;
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidEdges);
    }

    /// <summary>
    /// Test for parsing valid AGV graph message.
    /// </summary>
    [TestMethod]
    public void Parse_ValidInput_GraphValid()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 0f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        var graph = parser.Graph;

        parser.SerialNumber.ShouldBe(AgvTestIdentifier01);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        graph.Edges.Count.ShouldBe(1);
    }

    /// <summary>
    /// Test for parsing valid AGV graph message with optional input
    /// </summary>
    [TestMethod]
    public void Parse_WithOptionalInput_GraphValid()
    {
        const string mapId = "MAP01";

        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 0f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, mapId, Guid.NewGuid()));
        var graph = parser.Graph;

        parser.SerialNumber.ShouldBe(AgvTestIdentifier01);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.CorrelationId.ShouldBe(AgvTestIdentifier01.ToGuid());
    }

    /// <summary>
    /// Test for parsing invalid AGV graph and try to get velocity information
    /// </summary>
    [TestMethod]
    public void Parse_InvalidGraph_ExceptionWhenGettingVelocityInformation()
    {
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, Array.Empty<MqttNode>(), Array.Empty<MqttEdge>(), string.Empty));

        Should.Throw<InvalidOperationException>(() => parser.ConsiderVelocity);
        Should.Throw<InvalidOperationException>(() => parser.AverageVelocity);

        var graph = parser.Graph;
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidNodes);
    }

    /// <summary>
    /// Test for parsing valid AGV graph message without velocity information.
    /// </summary>
    [TestMethod]
    public void Parse_WithoutVelocityButAccessVelocity_ExceptionButGraphValid()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 0f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 0 }
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, string.Empty));

        Should.Throw<InvalidOperationException>(() => parser.AverageVelocity);

        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier01);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
    }

    /// <summary>
    /// Test for parsing AGV graph message.
    /// </summary>
    [TestMethod]
    public void Parse_WithUnidirectionalEdge_ExpectOne2OneConversion()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));

        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier02);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        graph.Edges.Count.ShouldBe(1);
    }

    /// <summary>
    /// Test for parsing AGV graph message.
    /// </summary>
    [TestMethod]
    public void Parse_WithBidirectionalEdge_ExpectOne2TwoConversion()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 }
        };

        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));

        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier02);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        // Edge has to be duplicated because it is bidirectional.
        graph.Edges.Count.ShouldBe(2);
    }

    [TestMethod]
    public void Parse_Bezier1_NotSingleControlPointProvided_ThrowsException()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 , EdgeType = "Bezier1", ControlPoints = new List<MqttControlPoint>{new(), new()}}
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        Should.Throw<MqttGraphParsingException>(() => parser.Graph.Edges);
    }

    [TestMethod]
    public void Parse_Bezier1_ReturnsBezier1Curve()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 , EdgeType = "Bezier1", ControlPoints = new List<MqttControlPoint>{new()}}
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier02);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        graph.Edges.Count.ShouldBe(1);
        graph.Edges.Select(e => e.Variant).First().ShouldBe(EdgeType.Bezier1);
    }

    [TestMethod]
    public void Parse_Bezier2_NotTwoControlPointsProvided_ThrowsException()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 , EdgeType = "Bezier2", ControlPoints = new List<MqttControlPoint>{new()}}
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        Should.Throw<MqttGraphParsingException>(() => parser.Graph.Edges);
    }

    [TestMethod]
    public void Parse_Bezier2_ReturnsBezier2Curve()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 , EdgeType = "Bezier2", ControlPoints = new List<MqttControlPoint>{new(), new()}}
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier02);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        graph.Edges.Count.ShouldBe(1);
        graph.Edges.Select(e => e.Variant).First().ShouldBe(EdgeType.Bezier2);
    }

    [TestMethod]
    public void Parse_Bezier2_Bidirectional_ReverseControlPoints()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 , EdgeType = "Bezier2",
                ControlPoints = new List<MqttControlPoint>{new(){X=1,Y=1}, new(){X=2,Y=2}}}
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier02);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        var edge = graph.Edges.First();
        edge.Variant.ShouldBe(EdgeType.Bezier2);
        var reverseEdge = graph.Edges.Last();
        reverseEdge.Variant.ShouldBe(EdgeType.Bezier2);
        edge.VariantParameters.ShouldBe(reverseEdge.VariantParameters.Reverse());
    }

    [TestMethod]
    public void Parse_Polypath_NoControlPointsProvided_ThrowsException()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 , EdgeType = "Polypath"}
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        Should.Throw<MqttGraphParsingException>(() => parser.Graph.Edges);
    }

    [TestMethod]
    public void Parse_Polypath_ReturnsPolypathCurve()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 , EdgeType = "Polypath", ControlPoints = new List<MqttControlPoint>{new(), new()}}
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier02);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        graph.Edges.Count.ShouldBe(1);
        graph.Edges.Select(e => e.Variant).First().ShouldBe(EdgeType.Polypath);
    }

    [TestMethod]
    public void Parse_Polypath_Bidirectional_ReverseControlPoints()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = true, MaxVelocity = 1 , EdgeType = "Polypath", 
                ControlPoints = new List<MqttControlPoint>{new(){X=1,Y=1}, new(){X=2,Y=2}}}
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier02);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        var edge = graph.Edges.First();
        edge.Variant.ShouldBe(EdgeType.Polypath);
        var reverseEdge = graph.Edges.Last();
        reverseEdge.Variant.ShouldBe(EdgeType.Polypath);
        edge.VariantParameters.ShouldBe(reverseEdge.VariantParameters.Reverse());
    }

    [TestMethod]
    public void Parse_Slanted_ReturnsSlantedCurve()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 , EdgeType = "Slanted", ExitAngle = 1.5f, ExitDistance = 2}
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier02);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        graph.Edges.Count.ShouldBe(1);
        graph.Edges.Select(e => e.Variant).First().ShouldBe(EdgeType.Slanted);
    }

    [TestMethod]
    public void Parse_UnknownEdgeType_ReturnDirectCurve()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1, EdgeType = "unknown"}
        };
        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));

        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier02);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        graph.Edges.Count.ShouldBe(1);
        graph.Edges.Select(e => e.Variant).First().ShouldBe(EdgeType.Direct);
    }

    /// <summary>
    /// Test for parsing AGV graph message.
    /// </summary>
    [TestMethod]
    public void Parse_WithVelocity_ExpectNoAdditionalEdges()
    {
        var nodes = new MqttNode[]
        {
            // Square with 4 edges.
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 10f, Y = 0f, Z = 0f },
            new() { NodeId = 3, X = 10f, Y = 10f, Z = 0f },
            new() { NodeId = 4, X = 0f, Y = 10f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 10 },
            new() { EdgeId = 2, StartId = 2, EndId = 3, Bidirectional = false, MaxVelocity = 20 },
            new() { EdgeId = 3, StartId = 3, EndId = 4, Bidirectional = false, MaxVelocity = 40 },
            new() { EdgeId = 4, StartId = 4, EndId = 1, Bidirectional = false, MaxVelocity = 10 }
        };

        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));

        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvTestIdentifier02);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(4);
        graph.Edges.Count.ShouldBe(4);
        //              4x distance / 4x durations
        parser.AverageVelocity.ShouldBe(4f * 10 / (10f / 10 + 10f / 20 + 10f / 40 + 10f / 10), 0.00001);
    }

    /// <summary>
    /// Test for parsing AGV graph message with invalid nodes.
    /// </summary>
    [TestMethod]
    public void Parse_WithInvalidNodes_GraphInvalid()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            // Ambiguous node id
            new() { NodeId = 1, X = 10f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 1, Bidirectional = false, MaxVelocity = 10 }
        };

        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);

        var graph = parser.Graph;
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidNodes);
    }

    /// <summary>
    /// Test for parsing AGV graph message with invalid edges.
    /// </summary>
    [TestMethod]
    public void Parse_WithInvalidEdges_GraphInvalid()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 10f, Y = 0f, Z = 0f }
        };
        MqttEdge[] edges = Array.Empty<MqttEdge>();

        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier01, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);

        var graph = parser.Graph;
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidEdges);
    }

    [TestMethod]
    public void Parse_WithDuplicateEdges_GraphInvalid()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = 2, X = 10f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = 1, StartId = 1, EndId = 1, Bidirectional = false, MaxVelocity = 10 },
            // Duplicate edge
            new() { EdgeId = 1, StartId = 1, EndId = 1, Bidirectional = false, MaxVelocity = 10 }
        };

        var parser = new GraphParser(new MqttGraph(AgvTestIdentifier02, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);

        var graph = parser.Graph;
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidEdges);
    }
}