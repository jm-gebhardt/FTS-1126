using Gebhardt.Acc.Common.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// Exception thrown when a node could not be reserved
/// </summary>
public class ReserveNodeFailedException : NodeStateException
{
    /// <inheritdoc cref="ReserveNodeFailedException"/>>
    /// <param name="nodeId">The nodeId of the node which could not be locked</param>
    public ReserveNodeFailedException(Guid nodeId) : base(nodeId.ToString(), "lock")
    {
    }
}