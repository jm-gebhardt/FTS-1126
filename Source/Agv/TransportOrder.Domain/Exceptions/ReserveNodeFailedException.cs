using Gebhardt.StoreWare.Agv.Common.Exceptions;

namespace TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception thrown when a node could not be reserved
/// </summary>
public class ReserveNodeFailedException : NodeStateException
{
    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="nodeId">The nodeId of the node which could not be locked</param>
    public ReserveNodeFailedException(string nodeId) : base(nodeId, "lock")
    {
    }
}