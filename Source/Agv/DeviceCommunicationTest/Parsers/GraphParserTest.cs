using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Exceptions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Parsers;

/// <summary>
/// Tests for <see cref="GraphParser"/>.
/// </summary>
[TestClass]
public class GraphParserTest
{
    private const string AgvSerialNumber1 = "#2410#";
    private const string AgvSerialNumber2 = "#0815#";

    [TestMethod]
    public void GetSerialNumber_WithoutCallingParse_ThrowsInvalidOperationException()
    {
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, [], [], string.Empty));
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);
    }

    [TestMethod]
    public void GetConsiderVelocity_WithoutCallingParse_ThrowsInvalidOperationException()
    {
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, [], [], string.Empty));
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);
    }

    [TestMethod]
    public void GetAverageVelocity_WithoutCallingParse_ThrowsInvalidOperationException()
    {
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, [], [], string.Empty));
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);
    }

    [TestMethod]
    public void GetGraph_WithoutCallingParse_ThrowsInvalidOperationException()
    {
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, [], [], string.Empty));
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);
    }

    /// <summary>
    /// Test for parsing invalid AGV graph message with serial number missing.
    /// </summary>
    [TestMethod]
    public void Parse_WithoutSerialNumber_GraphInvalid()
    {
        var parser = new GraphParser(new MqttGraph("", DateTimeOffset.UtcNow, [], [], string.Empty));
        parser.Parse();
        var graph = parser.Graph;

        graph.Usability.ShouldBe(AgvGraph.GraphUsability.SerialNumberMissing);
    }

    /// <summary>
    /// Test for parsing invalid AGV graph message with missing nodes and edges.
    /// </summary>
    [TestMethod]
    public void Parse_WithEmptyNodes_GraphInvalid()
    {
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, [], [], string.Empty));
        parser.Parse();
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, nodes, [], string.Empty));
        parser.Parse();
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 0f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1 }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        var graph = parser.Graph;

        parser.SerialNumber.ShouldBe(AgvSerialNumber1);
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 0f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1 }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, nodes, edges, mapId, Guid.NewGuid()));
        parser.Parse();
        var graph = parser.Graph;

        parser.SerialNumber.ShouldBe(AgvSerialNumber1);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.AgvCorrelationId.ShouldBe(AgvSerialNumber1.ToGuid());
    }

    /// <summary>
    /// Test for parsing invalid AGV graph and try to get velocity information
    /// </summary>
    [TestMethod]
    public void Parse_InvalidGraph_ExceptionWhenGettingVelocityInformation()
    {
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, [], [], string.Empty));
        parser.Parse();

        Should.Throw<InvalidOperationException>(() => parser.IsVelocityDataValid);
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 0f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 0 }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();

        Should.Throw<InvalidOperationException>(() => parser.AverageVelocity);

        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber1);
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1 }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();

        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1 }
        };

        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();

        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        // Edge has to be duplicated because it is bidirectional.
        graph.Edges.Count.ShouldBe(2);
    }

    [TestMethod]
    public void Parse_Bezier2_NotSingleControlPointProvided_ThrowsException()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1, EdgeType = "Bezier2", ControlPoints = new List<MqttControlPoint> { new(), new() } }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        Should.Throw<MqttGraphParsingException>(() => parser.Parse());
    }

    [TestMethod]
    public void Parse_Bezier2_ReturnsBezier2Curve()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1, EdgeType = "Bezier2", ControlPoints = new List<MqttControlPoint> { new() } }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        graph.Edges.Count.ShouldBe(1);
        graph.Edges.Select(e => e.Variant).First().ShouldBe(EdgeType.Bezier2);
    }

    [TestMethod]
    public void Parse_Bezier3_NotTwoControlPointsProvided_ThrowsException()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1, EdgeType = "Bezier3", ControlPoints = new List<MqttControlPoint> { new() } }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        Should.Throw<MqttGraphParsingException>(() => parser.Parse());
    }

    [TestMethod]
    public void Parse_Bezier3_ReturnsBezier3Curve()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1, EdgeType = "Bezier3", ControlPoints = new List<MqttControlPoint> { new(), new() } }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        graph.Edges.Count.ShouldBe(1);
        graph.Edges.Select(e => e.Variant).First().ShouldBe(EdgeType.Bezier3);
    }

    [TestMethod]
    public void Parse_Bezier3_Bidirectional_ReverseControlPoints()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new()
            {
                EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1, EdgeType = "Bezier3",
                ControlPoints = new List<MqttControlPoint> { new() { X = 1, Y = 1 }, new() { X = 2, Y = 2 } }
            }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        var edge = graph.Edges.First();
        edge.Variant.ShouldBe(EdgeType.Bezier3);
        var reverseEdge = graph.Edges.Last();
        reverseEdge.Variant.ShouldBe(EdgeType.Bezier3);
        edge.VariantParameters.ShouldBe(reverseEdge.VariantParameters.Reverse());
    }

    [TestMethod]
    public void Parse_Bezier5_NotFourControlPointsProvided_ThrowsException()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1, EdgeType = "Bezier5", ControlPoints = new List<MqttControlPoint> { new() } }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        Should.Throw<MqttGraphParsingException>(() => parser.Parse());
    }

    [TestMethod]
    public void Parse_Bezier5_ReturnsBezier5Curve()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1, EdgeType = "Bezier5", ControlPoints = new List<MqttControlPoint> { new(), new(), new(), new() } }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        graph.Edges.Count.ShouldBe(1);
        graph.Edges.Select(e => e.Variant).First().ShouldBe(EdgeType.Bezier5);
    }

    [TestMethod]
    public void Parse_Bezier5_Bidirectional_ReverseControlPoints()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new()
            {
                EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1, EdgeType = "Bezier5",
                ControlPoints = new List<MqttControlPoint> { new() { X = 1, Y = 1 }, new() { X = 2, Y = 2 }, new() { X = 3, Y = 3 }, new() { X = 4, Y = 4 } }
            }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        graph.Nodes.Count.ShouldBe(2);
        var edge = graph.Edges.First();
        edge.Variant.ShouldBe(EdgeType.Bezier5);
        var reverseEdge = graph.Edges.Last();
        reverseEdge.Variant.ShouldBe(EdgeType.Bezier5);
        edge.VariantParameters.ShouldBe(reverseEdge.VariantParameters.Reverse());
    }

    [TestMethod]
    public void Parse_Polypath_NoControlPointsProvided_ThrowsException()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1, EdgeType = "Polypath" }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        Should.Throw<MqttGraphParsingException>(() => parser.Parse());
    }

    [TestMethod]
    public void Parse_Polypath_ReturnsPolypathCurve()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1, EdgeType = "Polypath", ControlPoints = new List<MqttControlPoint> { new(), new() } }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new()
            {
                EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1, EdgeType = "Polypath",
                ControlPoints = new List<MqttControlPoint> { new() { X = 1, Y = 1 }, new() { X = 2, Y = 2 } }
            }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1, EdgeType = "Slanted", ExitAngle = 1.5f, ExitDistance = 2 }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 100f, Y = 100f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 1, EdgeType = "unknown" }
        };
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();

        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 10f, Y = 0f, Z = 0f },
            new() { NodeId = "3", X = 10f, Y = 10f, Z = 0f },
            new() { NodeId = "4", X = 0f, Y = 10f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 10 },
            new() { EdgeId = "2", StartId = "2", EndId = "3", Bidirectional = false, MaxVelocity = 20 },
            new() { EdgeId = "3", StartId = "3", EndId = "4", Bidirectional = false, MaxVelocity = 40 },
            new() { EdgeId = "4", StartId = "4", EndId = "1", Bidirectional = false, MaxVelocity = 10 }
        };

        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();

        var graph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber2);
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            // Ambiguous node id
            new() { NodeId = "1", X = 10f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "1", Bidirectional = false, MaxVelocity = 10 }
        };

        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
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
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 10f, Y = 0f, Z = 0f }
        };
        MqttEdge[] edges = [];

        var parser = new GraphParser(new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);

        var graph = parser.Graph;
        graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidEdges);
    }

    [TestMethod]
    public void Parse_WithDuplicateEdges_GraphInvalid()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 10f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "1", Bidirectional = false, MaxVelocity = 10 },
            // Duplicate edge
            new() { EdgeId = "1", StartId = "1", EndId = "1", Bidirectional = false, MaxVelocity = 10 }
        };

        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        parser.Parse();
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);

        parser.Graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidEdges);
    }

    [TestMethod]
    public void Parse_ModificationDate_SetOnNodesAndEdges()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 10f, Y = 0f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = false, MaxVelocity = 10 }
        };

        var modificationDate = DateTimeOffset.UtcNow;
        var parser = new GraphParser(new MqttGraph(AgvSerialNumber2, modificationDate, nodes, edges, string.Empty));
        parser.Parse();

        foreach (var node in parser.Graph.Nodes)
        {
            node.TimeStamp.ShouldBe(modificationDate.UtcDateTime, TimeSpan.FromSeconds(1));
        }

        foreach (var edge in parser.Graph.Edges)
        {
            edge.TimeStamp.ShouldBe(modificationDate.UtcDateTime, TimeSpan.FromSeconds(1));
        }
    }
}