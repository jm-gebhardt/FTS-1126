using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.MessageParsers;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Parsers;

/// <summary>
/// Tests for <see cref="StationListParser"/>.
/// </summary>
[TestClass]
public class StationListParserTest
{
    private const string AgvSerialNumber1 = "#2410#";
    private const string AgvSerialNumber2 = "#0815#";

    /// <summary>
    /// Test recognition of missing input at all.
    /// </summary>
    [TestMethod]
    public void Parse_WithInvalidInputAtAll_GraphInvalid()
    {
        var parser = new StationListParser(
            new MqttStationList("", DateTimeOffset.UtcNow, Array.Empty<Station>()),
            new MqttGraph("", DateTimeOffset.UtcNow, Array.Empty<MqttNode>(), Array.Empty<MqttEdge>(), string.Empty));
        var graph = parser.Graph;

        graph.Usability.ShouldBe(AgvGraph.GraphUsability.SerialNumberMissing);
    }

    /// <summary>
    /// Test recognition of missing nodes.
    /// </summary>
    [TestMethod]
    public void Parse_WithMissingGraphEdgeAndStation_GraphInvalid()
    {
        var parser = new StationListParser(
            new MqttStationList(AgvSerialNumber1, DateTimeOffset.UtcNow, Array.Empty<Station>()),
            new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, Array.Empty<MqttNode>(), Array.Empty<MqttEdge>(), string.Empty));
        var graph = parser.Graph;

        graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidNodes);
        Should.Throw<InvalidOperationException>(() => parser.SerialNumber);
    }

    /// <summary>
    /// Test recognition of missing edges
    /// </summary>
    [TestMethod]
    public void Parse_WithMissingEdgeAndStation_NoExceptionWhileCheckingValidity()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 9f, Y = 9f, Z = 0f }
        };
        var parser = new StationListParser(
            new MqttStationList(AgvSerialNumber1, DateTimeOffset.UtcNow, Array.Empty<Station>()),
            new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, nodes, Array.Empty<MqttEdge>(), string.Empty));
        var graph = parser.Graph;

        graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidEdges);
    }

    /// <summary>
    /// Test recognition of missing stations
    /// </summary>
    [TestMethod]
    public void Parse_WithMissingStation__GraphInvalid()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 9f, Y = 9f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1 }
        };
        var parser = new StationListParser(
            new MqttStationList(AgvSerialNumber1, DateTimeOffset.UtcNow, Array.Empty<Station>()),
            new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        var graph = parser.Graph;

        graph.Usability.ShouldBe(AgvGraph.GraphUsability.InvalidStations);
    }

    /// <summary>
    /// Test minimal valid graph
    /// </summary>
    [TestMethod]
    public void Parse_WithMinimumContent_GraphValid()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 9f, Y = 9f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1 }
        };
        var stations = new Station[]
        {
            new(Function.Pick, "P01", new Position(0f, 0f, 0f), "")
        };
        var parser = new StationListParser(
            new MqttStationList(AgvSerialNumber1, DateTimeOffset.UtcNow, stations),
            new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        var graph = parser.Graph;

        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        parser.SerialNumber.ShouldBe(AgvSerialNumber1);
        //              3 = 2 nodes + 1 station
        graph.Nodes.Count.ShouldBe(3);
        //              4 = 2 * 2 bidirectional edges
        graph.Edges.Count.ShouldBe(4);
    }

    /// <summary>
    /// Test valid graph with additional optional content
    /// </summary>
    [TestMethod]
    public void Parse_WithOptionalContent_GraphValid()
    {
        const string mapId = "MAP01";

        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 9f, Y = 9f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1 }
        };
        var stations = new Station[]
        {
            new(Function.Pick, "P01", new Position(0f, 0f, 0f), "")
        };
        var parser = new StationListParser(
            new MqttStationList(AgvSerialNumber1, DateTimeOffset.UtcNow, stations),
            new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, nodes, edges, mapId, Guid.NewGuid()));
        var graph = parser.Graph;

        graph.Usability.ShouldBe(AgvGraph.GraphUsability.Valid);
        parser.SerialNumber.ShouldBe(AgvSerialNumber1);
        graph.CorrelationId.ShouldBe(AgvSerialNumber1.ToGuid());
    }

    /// <summary>
    /// Test minimal valid graph
    /// </summary>
    [TestMethod]
    public void Parse_WithSerialNumberMismatch_GraphInvalid()
    {
        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 9f, Y = 9f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1 }
        };
        var stations = new Station[]
        {
            new(Function.Pick, "P01", new Position(0f, 0f, 0f), "")
        };
        var parser = new StationListParser(
            new MqttStationList(AgvSerialNumber1, DateTimeOffset.UtcNow, stations),
            new MqttGraph(AgvSerialNumber2, DateTimeOffset.UtcNow, nodes, edges, string.Empty));
        var graph = parser.Graph;

        graph.Usability.ShouldBe(AgvGraph.GraphUsability.SerialNumberMismatch);
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

        var nodes = new MqttNode[]
        {
            new() { NodeId = "1", X = 0f, Y = 0f, Z = 0f },
            new() { NodeId = "2", X = 9f, Y = 9f, Z = 0f }
        };
        var edges = new MqttEdge[]
        {
            new() { EdgeId = "1", StartId = "1", EndId = "2", Bidirectional = true, MaxVelocity = 1 }
        };
        var stations = new Station[]
        {
            new(Function.Both, "S1", new Position(0f, 2f, -2f), ""),
            new(Function.Both, "S2", new Position(0f, 7f, 11f), ""),
            new(Function.Both, "S3", new Position(0f, 6f, -2f), "")
        };
        var parser = new StationListParser(
            new MqttStationList(AgvSerialNumber1, DateTimeOffset.UtcNow, stations),
            new MqttGraph(AgvSerialNumber1, DateTimeOffset.UtcNow, nodes, edges, string.Empty));

        var agvGraph = parser.Graph;
        parser.SerialNumber.ShouldBe(AgvSerialNumber1);

        agvGraph.Nodes.Count.ShouldBe(5);
        agvGraph.Edges.Count.ShouldBe(8);

        // Average velocity for the single edge is 1 (metric[t] = SQRT(9^2+9^2)/1 = 12.72792.

        // Distance S1->1 is 2 (metric[t] = SQRT(2^2+2^2)/1 = 2,82843
        float metric = (float)Math.Sqrt(8);
        agvGraph.Edges.First(x => x.FromNodeId == "S1").Length.ShouldBe(metric, 0.00001);
        agvGraph.Edges.First(x => x.ToNodeId == "S1").Length.ShouldBe(metric, 0.00001);

        // Distance S2->2 is 2 (metric[t] = SQRT(2^2+2^2)/1 = 2,82843
        metric = (float)Math.Sqrt(8);
        agvGraph.Edges.First(x => x.FromNodeId == "S2").Length.ShouldBe(metric, 0.00001);
        agvGraph.Edges.First(x => x.ToNodeId == "S2").Length.ShouldBe(metric, 0.00001);

        // Distance S3->1 is 2 (metric[t] = SQRT(6^2+2^2)/1 = 6,32456
        metric = (float)Math.Sqrt(40);
        agvGraph.Edges.First(x => x.FromNodeId == "S3").Length.ShouldBe(metric, 0.00001);
        agvGraph.Edges.First(x => x.ToNodeId == "S3").Length.ShouldBe(metric, 0.00001);
    }
}