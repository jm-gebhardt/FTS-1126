using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using Gebhardt.Acc.TransportOrder.Domain.Models.Queue;
using Gebhardt.Acc.TransportOrder.Messages.Profiles;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class QueueGenerator
{
    public static Queue GenerateQueueWithCapacity(string name = "queue", ICollection<NodeLockState>? lockStates = null, int? size = null)
    {
        lockStates ??= [NodeLockState.Occupied, NodeLockState.Reserved, NodeLockState.Free];
        NodeLock[] nodeLocks = lockStates.Select((n, _) =>
                new NodeLock(Guid.NewGuid().ToGraphNode(), n, false, n == NodeLockState.Free ? null : Guid.NewGuid(), DateTime.UtcNow))
            .ToArray();
        size ??= nodeLocks.Count(n => n.NodeState != NodeLockState.Free);
        return GenerateQueue(name, nodeStates: nodeLocks, size: size);
    }

    public static Queue GenerateEmptyQueue(string name = "queue", int nodes = 3, int? size = null)
    {
        NodeLock[] nodeLocks = Enumerable.Range(0, nodes).Select(_ => new NodeLock(Guid.NewGuid().ToGraphNode(), NodeLockState.Free, false, null, DateTime.UtcNow)).ToArray();
        return GenerateQueue(name, nodeStates: nodeLocks, size: size);
    }

    public static Queue GenerateFullQueue(string name = "queue", int nodes = 3)
    {
        NodeLock[] nodeLocks = Enumerable.Range(0, nodes).Select(_ => new NodeLock(Guid.NewGuid().ToGraphNode(), NodeLockState.Occupied, false, Guid.NewGuid(), DateTime.UtcNow)).ToArray();
        return GenerateQueue(name, nodeStates: nodeLocks);
    }

    private static Queue GenerateQueue(string name = "a queue", ICollection<NodeLock>? nodeStates = null, int? size = null)
    {
        if (nodeStates != null && size == null)
        {
            size = nodeStates.Count(n => n.NodeState != NodeLockState.Free);
        }
        var mapper = MappingHelper.GetMapper(new GraphProfile());
        var queue = new Queue(name)
        {
            AgvFillQuantity = size ?? 0
        };
        if (nodeStates == null)
        {
            return queue;
        }
        foreach (var node in nodeStates.Select((state, index) => new { index, state }))
        {
            queue.AddNode(node.state.NodeId, node.index);
            var queueNode = queue.Nodes.Single(n => n.Position == node.index);
            if (node.state is { NodeState: NodeLockState.Reserved, AgvCorrelationId: not null })
            {
                queueNode.Reserve(node.state.AgvCorrelationId.Value);
            }
            if (node.state is { NodeState: NodeLockState.Occupied, AgvCorrelationId: not null })
            {
                queueNode.Occupy(node.state.AgvCorrelationId.Value);
            }
            var graphNode = node.state.NodeId.ToGraphNode();
            queueNode.Node = mapper.Map<EfNode>(graphNode);
        }
        queue.Nodes.OrderBy(n => n.Position).Where(n => n.OccupiedAgv != null).Select(n => n.OccupiedAgv!.Value).ToList().ForEach(queue.ConsideredAgvs.Add);

        queue.ClearChangedFlag();
        return queue;
    }
}