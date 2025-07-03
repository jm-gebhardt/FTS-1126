using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Dtos.AgvActions;

/// <summary>
/// Provides a dto for agv actions.
/// </summary>
[PublicAPI]
public record AgvActionDto
{
    /// <summary>
    /// Unique CorrelationId of this action
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Returns the Id of the parent element 
    /// </summary>
    public Guid ParentId { get; init; }

    /// <summary>
    /// The sequence or natural order, if any, in which the object has to integrate 
    /// </summary>
    public int Sequence { get; init; }

    /// <summary>
    /// The current state of the action
    /// </summary>
    public ActionState State { get; init; }

    /// <summary>
    /// The type of the action, e.g. Pick or Drop
    /// </summary>
    public ActionType ActionType { get; init; }

    /// <summary>
    /// Target node id of the action
    /// </summary>
    public string TargetNodeId { get; init; }

    /// <summary>
    /// Type of the target node of the action
    /// </summary>
    public AgvGraph.NodeType TargetNodeType { get; init; }

    /// <summary>
    /// The nodes of the trip
    /// </summary>
    public string[] Nodes { get; init; }

    /// <summary>
    /// Date the action was created (UTC)
    /// </summary>
    public DateTime CreatedDate { get; init; }

    /// <summary>
    /// Date the action has been started (UTC)
    /// </summary>
    public DateTime? StartedDate { get; init; }

    /// <summary>
    /// Date the action has been completed (UTC)
    /// </summary>
    public DateTime? CompletedDate { get; init; }

    /// <summary>
    /// Date the action has been paused (UTC)
    /// </summary>
    public DateTime? PausedDate { get; init; }

    /// <summary>
    /// Duration (in seconds) that the action was paused for
    /// </summary>
    public long PausedDuration { get; init; }

    /// <summary>
    /// Indicates whether the action group is marked as skipped.
    /// </summary>
    public bool IsSkipped { get; set; }

    /// <summary>
    /// Information that will be passed to the agv.
    /// </summary>
    public Dictionary<InfoToAgvKey, string>? InfoToAgv { get; init; }

    /// <summary>
    /// Additional information for this action, often used for external systems.
    /// </summary>
    public string? AdditionalInfo { get; set; }

    /// <summary>
    /// AgvAction model
    /// <param name="correlationId">Correlation ID</param>
    /// <param name="parentId">Action group ID</param>
    /// <param name="sequence">Sequence number</param>
    /// <param name="state">The current state of the action</param>
    /// <param name="actionType">The type of the action, e.g. Pick or Drop</param>
    /// <param name="targetNodeId">Target node id</param>
    /// <param name="nodes">The nodes of the trip</param>
    /// <param name="createdDate">Date the action was created (UTC)</param>
    /// <param name="startedDate">Date the action was started (UTC)</param>
    /// <param name="completedDate">Date the action was completed (UTC)</param>
    /// <param name="pausedDate"> Date the action was paused (UTC)</param>
    /// <param name="infoToAgv">The information that will be passed to the agv.</param>
    /// </summary>
    public AgvActionDto
    (
        Guid correlationId,
        Guid parentId,
        int sequence,
        ActionState state,
        ActionType actionType,
        string targetNodeId,
        string[] nodes,
        DateTime createdDate,
        DateTime? startedDate,
        DateTime? completedDate,
        DateTime? pausedDate,
        Dictionary<InfoToAgvKey, string> infoToAgv
    )
    {
        CorrelationId = correlationId;
        ParentId = parentId;
        Sequence = sequence;
        State = state;
        ActionType = actionType;
        TargetNodeId = targetNodeId;
        Nodes = nodes;
        CreatedDate = createdDate;
        StartedDate = startedDate;
        CompletedDate = completedDate;
        PausedDate = pausedDate;
        InfoToAgv = infoToAgv;
    }

    /// <summary>
    /// ctor called by AutoMapper.
    /// </summary>
    private AgvActionDto()
    {
        TargetNodeId = null!;
        Nodes = null!;
    }
}