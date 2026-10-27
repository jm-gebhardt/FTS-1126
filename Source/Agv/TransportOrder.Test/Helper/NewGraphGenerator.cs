using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

[PublicAPI]
public static class NewGraphGenerator
{
    public const string LevelId = "level";
    public const string StartNodeName = "start";
    public const string EndNodeName = "end";
    public const string UnknownNodeName = "unknown";
    public const string EdgeName = "start-end";
    public const string StationName = "station";

    public const string BackEdgeName = "end-start";

    public static readonly Guid RoutingAgvTypeId = Guid.NewGuid();
    public static readonly Guid RoutingLoadStateId = Guid.NewGuid();
    public static readonly Guid AgvTypeAId = Guid.Parse("10000000-0000-0000-0000-000000000000");
    public static readonly Guid AgvTypeBId = Guid.Parse("20000000-0000-0000-0000-000000000000");
    public static readonly Guid AgvTypeAUnloadedId = Guid.Parse("00000001-0000-0000-0000-000000000000");
    public static readonly Guid AgvTypeALoadedId = Guid.Parse("00000002-0000-0000-0000-000000000000");
    public static readonly Guid AgvTypeBUnloadedId = Guid.Parse("00000003-0000-0000-0000-000000000000");

    public static GraphSegment GenerateSegmentGraph(Guid? stationInteractionNodeId = null, bool withRoutingResources = false)
    {
        var graph = GenerateGraph(stationInteractionNodeId, withRoutingResources);

        return new GraphSegment(Guid.NewGuid(), graph.Nodes, graph.Edges, graph.Stations);
    }

    public static Graph GenerateGraph(Guid? stationInteractionNodeId = null, bool withRoutingResources = false)
    {
        var startNode = new Node(Guid.NewGuid(), StartNodeName, StartNodeName, StartNodeName,0, 0, LevelId, null);
        var endNode = new Node(Guid.NewGuid(), EndNodeName, EndNodeName, EndNodeName,1, 1, LevelId, null);
        var edge = new Edge(Guid.NewGuid(), EdgeName, EdgeName, EdgeName, startNode.Id, endNode.Id);
        var interactionNode = stationInteractionNodeId is null ? endNode : new Node(stationInteractionNodeId.Value, UnknownNodeName, UnknownNodeName, UnknownNodeName,2, 2, LevelId, null);
        var station = new Station(Guid.NewGuid(), StationName, StationName, StationName,1, 1, 0, 0.5, [interactionNode.Id]);
        List<Node> nodes = [startNode, endNode];
        List<Edge> edges = [edge];
        List<Station> stations = [station];

        if (withRoutingResources)
        {
            AddRoutingResources(startNode, edge);
        }

        return new Graph(nodes, edges, stations);
    }

    private static Trajectory CreateCurve()
    {
        return new Trajectory(new List<ControlPoint> { new(0, 0), new(1, 0), new(1, 1) }, [0, 0, 0, 1, 1, 1], 2);
    }

    public static Graph GenerateMultiStateGraph()
    {
        var startNode = new Node(Guid.NewGuid(), StartNodeName, StartNodeName, StartNodeName, 0, 0, LevelId, null);
        var endNode = new Node(Guid.NewGuid(), EndNodeName, EndNodeName, EndNodeName, 1, 1, LevelId, null);
        var edge = new Edge(Guid.NewGuid(), EdgeName, EdgeName, EdgeName, startNode.Id, endNode.Id);
        var backEdge = new Edge(Guid.NewGuid(), BackEdgeName, BackEdgeName, BackEdgeName, endNode.Id, startNode.Id);
        var station = new Station(Guid.NewGuid(), StationName, StationName, StationName, 0, 0, 0, 0.5, [startNode.Id]);

        (Guid AgvTypeId, Guid LoadStateId, RoutingActionType[] Actions, Trajectory? Trajectory)[] states =
        [
            (AgvTypeAId, AgvTypeAUnloadedId, [RoutingActionType.Pick], CreateCurve()),
            (AgvTypeAId, AgvTypeALoadedId, [RoutingActionType.Drop], CreateCurve()),
            (AgvTypeBId, AgvTypeBUnloadedId, [], null)
        ];
        foreach (var state in states)
        {
            var start = new RoutingNode(Guid.NewGuid(), state.AgvTypeId, state.LoadStateId, 0) { Actions = [.. state.Actions] };
            var end = new RoutingNode(Guid.NewGuid(), state.AgvTypeId, state.LoadStateId, 0);
            startNode.RoutingNodes.Add(start);
            endNode.RoutingNodes.Add(end);
            edge.RoutingEdges.Add(new RoutingEdge(Guid.NewGuid(), state.AgvTypeId, start.Id, end.Id, 1, 0, OrientationType.Tangential, null, null, false, null, null, state.Trajectory));
        }

        return new Graph([startNode, endNode], [edge, backEdge], [station]);
    }

    private static void AddRoutingResources(Node startNode, Edge edge)
    {
        var trajectory = new Trajectory(new List<ControlPoint> { new(0, 0), new(1, 1), new(2, 0) }, [0, 0, 0, 1, 1, 1]);

        var routingNode = new RoutingNode(Guid.NewGuid(), RoutingAgvTypeId, RoutingLoadStateId, 1);
        startNode.RoutingNodes.Add(routingNode);
        edge.RoutingEdges.Add(new RoutingEdge(Guid.NewGuid(), RoutingAgvTypeId, routingNode.Id, routingNode.Id, 1.5, 0.5, OrientationType.Tangential, 0.1, 0.2, true, 1.0, 0.5, trajectory));
        edge.RoutingEdges.Add(new ActionRoutingEdge(Guid.NewGuid(), RoutingAgvTypeId, routingNode.Id, routingNode.Id, 1.5, 0.5, OrientationType.Tangential, 0.1, 0.2, true, 1.0, 0.5, trajectory, RoutingActionType.Pick));
    }
}