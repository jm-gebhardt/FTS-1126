using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.TrafficManagement.NewGraph;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

/// <summary>
/// A helper class for generating routing resources for testing purposes.
/// </summary>
public static class RoutingResourceGenerator
{
    public static RoutingResource GenerateResource(params RoutingResource[] conflictingResources)
    {
        var parent = new Node(Guid.NewGuid(), "node", "n", 0, 0, null);
        var agvType = AgvTypeGenerator.GenerateAgvType();
        var resource = new RoutingNode(Guid.NewGuid(), parent, agvType, new LoadState(Guid.NewGuid(), false), 0);

        foreach (var conflictingResource in conflictingResources)
        {
            resource.AddConflictingResource(conflictingResource);
        }

        return resource;
    }

    public static RoutingNode CreateRoutingNode(AgvType agvType, double x = 0, double y = 0, Node? parentNode = null)
    {
        parentNode ??= new Node(Guid.NewGuid(), "node", "n", x, y, null);
        var routingNode = new RoutingNode(Guid.NewGuid(), parentNode, agvType, new LoadState(Guid.NewGuid(), false), 0);
        parentNode.AddChild(routingNode);
        return routingNode;
    }

    public static RoutingEdge CreateRoutingEdge(RoutingNode startNode, RoutingNode endNode, double length, double? maxSpeed = 1.0, Edge? parentEdge = null)
    {
        parentEdge ??= new Edge(Guid.NewGuid(), "edge", "e", startNode.ParentNode, endNode.ParentNode);

        var edge = new RoutingEdge(Guid.NewGuid(), parentEdge, startNode.AgvType, startNode, endNode, length, false, maxSpeed, null, null);
        startNode.AddOutgoingEdge(edge);
        endNode.AddIncomingEdge(edge);
        return edge;
    }
}