namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Node;

/// <summary>
/// Node State
/// </summary>
public enum NodeLockState
{
    /// <summary>Node is Free to ise</summary>
    Free,

    /// <summary>Node is Reserved for an agv</summary>
    Reserved,

    /// <summary>Node is occupied by an agv</summary>
    Occupied
}

/// <summary>
/// Lock state of a node
/// </summary>
/// <param name="NodeId"></param>
/// <param name="NodeState"></param>
/// <param name="IsLocked"></param>
/// <param name="AgvCorrelationId"></param>
/// <param name="ModifiedDate"></param>
public record NodeLock(string NodeId, NodeLockState NodeState, bool IsLocked, Guid? AgvCorrelationId, DateTime ModifiedDate);