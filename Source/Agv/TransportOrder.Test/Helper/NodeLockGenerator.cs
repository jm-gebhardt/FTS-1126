using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class NodeLockGenerator
{
    public static NodeLock GenerateNodeLock(
        string nodeName = "nodeId",
        NodeLockState nodeLockState = NodeLockState.Reserved,
        bool isLocked = false,
        Guid? agvCorrelationId = null,
        DateTime createdDate = default
    )
    {
        return new NodeLock(nodeName.ToGraphNode(), nodeLockState, isLocked, agvCorrelationId, createdDate);
    }
}