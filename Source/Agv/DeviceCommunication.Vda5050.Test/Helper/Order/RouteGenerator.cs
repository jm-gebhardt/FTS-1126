using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Orders;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Order;

/// <summary>
/// Provides a helper for generating <see cref="Route"/>s for unit testing.
/// </summary>
public static class RouteGenerator
{
    public static Route GenerateFromNodeIds(string[] nodeIds, int sequenceOffset=0)
    {
        List<OrderNode> nodes = [];
        List<OrderEdge> edges = [];
        string previousNode = string.Empty;
        foreach (var nodeId in nodeIds.Select((value, index) => new { value, index }))
        {
            var node = GenerateOrderNode(nodeId.value, sequenceOffset + nodeId.index*2);
            nodes.Add(node);
            if (nodeId.index > 0)
            {
                edges.Add(GenerateOrderEdge(node.SequenceId - 1, start: previousNode, end: node.NodeId));
            }
            previousNode = node.NodeId;
        }
        return new Route(nodes, edges);
    }

    public static OrderNode GenerateOrderNode(string nodeId, int sequenceId, ICollection<OrderAction>? actions=null)
    {
        return new OrderNode { NodeId = nodeId, SequenceId = sequenceId, Actions = actions ?? [], NodePosition = new OrderNodePosition() { X = 1, Y = 1 } };
    }

    public static OrderEdge GenerateOrderEdge(int sequenceId, string start, string end, ICollection<OrderAction>? actions = null)
    {
        return new OrderEdge() { EdgeId = $"edge{sequenceId}", SequenceId = sequenceId, Actions = actions ?? [],  StartNodeId = start, EndNodeId = end};
    }
}