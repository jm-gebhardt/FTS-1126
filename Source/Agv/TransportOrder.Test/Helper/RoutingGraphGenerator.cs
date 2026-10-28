using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class RoutingGraphGenerator
{
    public const string AgvTypeIdentifier = "TestAgvType";

    public static Trajectory GenerateTrajectory(int degree, params PolygonPoint[] controlPoints)
    {
        return new Trajectory(controlPoints.Select(point => new ControlPoint(point.X, point.Y)).ToList(), [], degree);
    }

    public static Node GenerateNode(double x, double y, double theta = 0, string? name = null, AgvType? agvType = null)
    {
        var nodeId = Guid.NewGuid();
        string nodeName = name ?? nodeId.ToString();
        var node = new Node(nodeId, nodeName, nodeName, nodeName,x, y, null, null);
        var type = agvType ?? AgvTypeGenerator.GenerateAgvType(AgvTypeIdentifier);
        var loadState = type.LoadStates.First(state => !state.IsLoaded);

        node.RoutingNodes.Add(new RoutingNode(Guid.NewGuid(), type.Id, loadState.Id, theta));

        return node;
    }

    public static RoutingNode GetRoutingNode(Node node)
    {
        return node.RoutingNodes.First();
    }

    public static RoutingEdge GetRoutingEdge(Edge edge)
    {
        return edge.RoutingEdges.First();
    }

    public static Edge GenerateEdge(Node startNode, Node endNode, double? length = null, Trajectory? trajectory = null)
    {
        var edgeId = Guid.NewGuid();
        string edgeName = edgeId.ToString();
        var edge = new Edge(edgeId, edgeName, edgeName, edgeName, startNode.Id, endNode.Id);
        var startRoutingNode = GetRoutingNode(startNode);
        var endRoutingNode = GetRoutingNode(endNode);
        double edgeLength = length ?? Math.Sqrt(Math.Pow(endNode.X - startNode.X, 2) + Math.Pow(endNode.Y - startNode.Y, 2));

        edge.RoutingEdges.Add(new RoutingEdge(Guid.NewGuid(), startRoutingNode.AgvTypeId, startRoutingNode.Id, endRoutingNode.Id,
            edgeLength, null, OrientationType.Tangential, null, null, false, null, null, trajectory));

        return edge;
    }

    public static (RoutingEdge RoutingEdge, Node StartNode, Node EndNode) GenerateRoutingEdge(double fromX, double fromY, double toX, double toY,
        double? length = null, Trajectory? trajectory = null)
    {
        var startNode = GenerateNode(fromX, fromY);
        var endNode = GenerateNode(toX, toY);

        return (GetRoutingEdge(GenerateEdge(startNode, endNode, length, trajectory)), startNode, endNode);
    }

    public static Graph GenerateGraph(IReadOnlyList<Node>? nodes = null, IReadOnlyList<Edge>? edges = null)
    {
        return new Graph(nodes ?? [], edges ?? [], []);
    }
}