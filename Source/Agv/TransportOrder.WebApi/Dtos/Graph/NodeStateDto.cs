using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages;
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
    public Guid? StationId { get; set; }

    /// <summary >User that locked station.</summary>
    public string? User { get; set; }

    /// <summary >Last modified date</summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary >Reason for locking station.</summary>
    public string? LockReason { get; set; }

    /// <summary >Reason description for locking station.</summary>
    public string? LockDescription { get; set; }

    /// <summary>
    /// AutoMapper
    /// </summary>
    public NodeStateDto()
    {
        NodeId = string.Empty;
    }

    /// <inheritdoc cref="NodeStateDto"/>>
    public NodeStateDto(string nodeId, NodeLockState nodeState, bool isLocked, Guid? agvCorrelationId, DateTime modifiedDate, Guid? stationId = null, string? user = null, string? lockReason = null, string? lockDescription = null)
    {
        NodeId = nodeId;
        NodeState = nodeState;
        IsLocked = isLocked;
        AgvCorrelationId = agvCorrelationId;
        StationId = stationId;
        User = user;
        ModifiedDate = modifiedDate;
        LockReason = lockReason;
        LockDescription = lockDescription;
    }
}