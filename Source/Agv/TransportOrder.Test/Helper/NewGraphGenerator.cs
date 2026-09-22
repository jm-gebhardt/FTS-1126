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

    public static readonly Guid RoutingAgvTypeId = Guid.NewGuid();
    public static readonly Guid RoutingLoadStateId = Guid.NewGuid();

    public static GraphSegment GenerateGraph(Guid? stationInteractionNodeId = null, bool withRoutingResources = false)
    {
        var startNode = new Node(Guid.NewGuid(), StartNodeName, StartNodeName, 0, 0, LevelId);
        var endNode = new Node(Guid.NewGuid(), EndNodeName, EndNodeName, 1, 1, LevelId);
        var edge = new Edge(Guid.NewGuid(), EdgeName, EdgeName, startNode.Id, endNode.Id);
        var interactionNode = stationInteractionNodeId is null ? endNode : new Node(stationInteractionNodeId.Value, UnknownNodeName, UnknownNodeName, 2, 2, LevelId);
        var station = new Station(Guid.NewGuid(), StationName, StationName, 1, 1, 0, 0.5, [interactionNode.Id]);
        List<Node> nodes = [startNode, endNode];
        List<Edge> edges = [edge];
        List<Station> stations = [station];

        if (withRoutingResources)
        {
            AddRoutingResources(startNode, edge);
        }

        return new GraphSegment(nodes, edges, stations);
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