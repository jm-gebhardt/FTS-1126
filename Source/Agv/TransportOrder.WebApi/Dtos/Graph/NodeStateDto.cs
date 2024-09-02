using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.TransportOrder.Domain.Models.Node;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.Graph;

/// <summary>
/// Provides a dto for node states.
/// </summary>
[PublicAPI]
public record NodeStateDto : DatedMessage
{
    /// <summary >The node id.</summary>
    public string NodeId { get; set; }
    
    /// <summary >The node lock state.</summary>
    public NodeLockState NodeState { get; set; }

    /// <summary >If true, node is locked.</summary>
    public bool IsLocked { get; set; }

    /// <summary >The correlation id of the agv the node is reserved for/ occupied by.</summary>
    public Guid? AgvCorrelationId { get; set; }

    /// <summary >The id of the station.</summary>
    public Guid StationId { get; set; }

    /// <inheritdoc cref="NodeStateDto"/>>
    public NodeStateDto()
    {
        StationId = Guid.NewGuid();
        NodeId = string.Empty;
    }

    /// <inheritdoc cref="NodeStateDto"/>>
    public NodeStateDto(string nodeId, NodeLockState nodeState, bool isLocked, Guid? agvCorrelationId, Guid stationId)
    {
        NodeId = nodeId;
        NodeState = nodeState;
        IsLocked = isLocked;
        AgvCorrelationId = agvCorrelationId;
        StationId = stationId;
    }
}