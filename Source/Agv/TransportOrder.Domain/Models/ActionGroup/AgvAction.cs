using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// Agv Action
/// </summary>
[PublicAPI]
public class AgvAction : IParentedBy<Guid>
{
    /// <summary>
    /// Unique CorrelationId of this action
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <inheritdoc/>
    public Guid ParentId { get; init; }

    /// <inheritdoc/>
    public int Sequence { get; init; }

    /// <summary>
    /// The current state of the action
    /// </summary>
    public ActionState State { get; set; } = ActionState.Created;

    /// <summary>
    /// The type of the action, e.g. Pick or Drop
    /// </summary>
    public ActionType ActionType { get; init; }

    /// <summary>
    /// The target of the action.
    /// </summary>
    public ActionTarget Target { get; set; } = null!;

    /// <summary>
    /// True, if the action is heading towards a position given
    /// by coordinates instead of node id.
    /// </summary>
    public bool IsMoveToCoordinate => Target.NodeId == null && Target.Position != null;

    /// <summary>
    /// The nodes of the trip.
    /// </summary>
    public string[] Nodes { get; set; } = [];

    /// <summary>
    /// The edges (Guids) of the trip.
    /// </summary>
    public Guid[] Edges { get; set; } = [];

    /// <summary>
    /// Date the action was created (UTC)
    /// </summary>
    public DateTime CreatedDate { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Date the action has been started (UTC)
    /// </summary>
    public DateTime? StartedDate { get; set; }

    /// <summary>
    /// Date the action has been completed (UTC)
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Date the action has been paused at (UTC)
    /// </summary>
    public DateTime? PausedDate { get; set; }

    /// <summary>
    /// Information that will be passed to the agv.
    /// </summary>
    public Dictionary<InfoToAgvKey, string>? InfoToAgv { get; set; }

    /// <summary>
    /// Duration that the action has been paused for.
    /// </summary>
    public TimeSpan PausedDuration { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// The duration of the action.
    /// </summary>
    public TimeSpan? Duration => CompletedDate - StartedDate - PausedDuration;

    /// <summary>
    /// Action is the first action of a group.
    /// </summary>
    public bool IsFirstAction => Sequence == 0;

    /// <summary>
    /// True if the action is complete, i.e. its target is not defined.
    /// </summary>
    public bool IsIncomplete => Target.NodeId == string.Empty;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="correlationId">Unique correlation ID of this action (see order action repository)</param>
    /// <param name="actionType">The type of the action, e.g. Pick or Drop</param>
    /// <param name="targetNodeId">Target node id of the action</param>
    /// <param name="parentId">The CorrelationId of the ActionGroup where this action belongs to</param>
    /// <param name="infoToAgv">Information that will be passed to the agv.</param>
    public AgvAction(Guid correlationId, ActionType actionType, string targetNodeId, Guid parentId, Dictionary<InfoToAgvKey, string>? infoToAgv = null)
    {
        CorrelationId = correlationId;
        ActionType = actionType;
        Target = new ActionTarget(targetNodeId);
        ParentId = parentId;
        InfoToAgv = infoToAgv;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="correlationId">Unique correlation ID of this action (see order action repository)</param>
    /// <param name="actionType">The type of the action, e.g. Pick or Drop</param>
    /// <param name="target">Target of the action</param>
    /// <param name="parentId">The CorrelationId of the ActionGroup where this action belongs to</param>
    /// <param name="infoToAgv">Information that will be passed to the agv.</param>
    public AgvAction(Guid correlationId, ActionType actionType, ActionTarget target, Guid parentId, Dictionary<InfoToAgvKey, string>? infoToAgv = null)
    {
        CorrelationId = correlationId;
        ActionType = actionType;
        Target = target;
        ParentId = parentId;
        InfoToAgv = infoToAgv;
    }

    /// <summary>
    /// Just to satisfy Entity Framework
    /// </summary>
    [UsedImplicitly]
    [JsonConstructor]
    public AgvAction()
    {
        CorrelationId = Guid.Empty;
        ActionType = ActionType.Pick;
        ParentId = Guid.Empty;
    }

    /// <summary>
    /// Updates the state of the Action and its Dates
    /// </summary>
    /// <param name="state">The new state</param>
    public void UpdateActionState(ActionState state)
    {
        if (State == state)
        {
            return;
        }

        if (State == ActionState.Paused && PausedDate != null)
        {
            PausedDuration += (TimeSpan)(DateTime.UtcNow - PausedDate);
            PausedDate = null;
        }

        State = state;

        SetDates();
    }

    /// <summary>
    /// Sets a target for the action.
    /// </summary>
    /// <param name="targetNodeId">The target node id.</param>
    /// <exception cref="SetActionTargetFailedException">Thrown, if a target was already set previously.</exception>
    public void SetTarget(string targetNodeId)
    {
        if (Target.NodeId != string.Empty)
        {
            throw new SetActionTargetFailedException(targetNodeId);
        }
        Target.NodeId = targetNodeId;
    }

    private void SetDates()
    {
        var now = DateTime.UtcNow;

        switch (State)
        {
            case ActionState.Started:
                StartedDate = now;
                break;

            case ActionState.Completed:
                CompletedDate = now;
                break;

            case ActionState.Failed:
            case ActionState.Aborted:
            case ActionState.Canceled:
            case ActionState.Invalid:
                CompletedDate = now;
                break;

            case ActionState.Paused:
                PausedDate = now;
                break;
        }
    }

    /// <summary>
    /// Simple string representation for logging.
    /// </summary>
    public override string ToString()
    {
        return $"Nodes: {string.Join(",", Nodes)} TargetNode: {Target.NodeId} CorrelationId: {CorrelationId}, ParentId: {ParentId}";
    }
}