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
    public NodeNotFoundException(Models.Agv.Agv agv, Guid nodeId)
        : base(nodeId.ToString(), "Agv CorrelationId", agv.CorrelationId)
    {
    }

    /// <inheritdoc cref="NodeNotFoundException"/>>
    public NodeNotFoundException(Guid agvCorrelationId, Guid nodeId)
        : base(nodeId.ToString(), nameof(agvCorrelationId), agvCorrelationId)
    {
    }

    /// <inheritdoc cref="NodeNotFoundException"/>>
    public NodeNotFoundException(string description) : base($"{description}")
    {
    }

    /// <inheritdoc cref="NodeNotFoundException"/>>
    public NodeNotFoundException(Guid? nodeId, string? nodeDescription = null) : base($"{nodeDescription} Node with Id {nodeId}")
    {
    }
}