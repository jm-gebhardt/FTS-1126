using Gebhardt.Acc.TransportOrder.Domain.Models.Node;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class NodeLockGenerator
{
    public static NodeLock GenerateNodeLock(
        string nodeId = "nodeId",
        NodeLockState nodeLockState = NodeLockState.Reserved,
        bool isLocked = false,
        Guid? agvCorrelationId = null,
        DateTime createdDate = default
    )
    {
        return new NodeLock(nodeId, nodeLockState, isLocked, agvCorrelationId, createdDate);
    }
}