using Gebhardt.Acc.Common.Domain.Exceptions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Exceptions;

/// <summary>
/// The Node was not found.
/// </summary>
public class NodeNotFoundException : NotFoundException
{
    /// <inheritdoc cref="NodeNotFoundException"/>>
    [PublicAPI]
    public NodeNotFoundException(Models.Agv.Agv agv, string description)
        : base(description, "Agv CorrelationId", agv.CorrelationId)
    {
    }

    /// <inheritdoc cref="NodeNotFoundException"/>>
    public NodeNotFoundException(Guid agvCorrelationId, string description)
        : base(description, nameof(agvCorrelationId), agvCorrelationId)
    {
    }

    /// <inheritdoc cref="NodeNotFoundException"/>>
    public NodeNotFoundException(string nodeId) : base($"Node with Id {nodeId}")
    {
    }
}