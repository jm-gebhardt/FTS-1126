using System.Text.Json.Serialization;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// Agv Action
/// </summary>
[PublicAPI]
public class AgvAction : IParentedBy<Guid>, IEquatable<AgvAction>
{
    /// <summary>
    /// Unique CorrelationId of this action
    /// </summary>
    public Guid CorrelationId { get; init; }

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
    public ActionType ActionType { get; private set; }

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
    /// Whether this <see cref="AgvAction"/> should be skipped.
    /// </summary>
    public bool ShouldSkip { get; private set; }

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
    public AgvAction()
    {
        CorrelationId = Guid.Empty;
        ParentId = Guid.Empty;
    }

    /// <inheritdoc cref="AgvAction"/>>
    [JsonConstructor]
    public AgvAction(
        Guid correlationId,
        Guid parentId,
        int sequence,
        ActionState state,
        ActionType actionType,
        ActionTarget target,
        string[] nodes,
        Guid[] edges,
        DateTime createdDate,
        DateTime? startedDate,
        DateTime? completedDate,
        DateTime? pausedDate,
        Dictionary<InfoToAgvKey, string>? infoToAgv,
        TimeSpan pausedDuration,
        bool shouldSkip
    )
    {
        CorrelationId = correlationId;
        ParentId = parentId;
        Sequence = sequence;
        State = state;
        ActionType = actionType;
        Target = target;
        Nodes = nodes;
        Edges = edges;
        CreatedDate = createdDate;
        StartedDate = startedDate;
        CompletedDate = completedDate;
        PausedDate = pausedDate;
        InfoToAgv = infoToAgv;
        PausedDuration = pausedDuration;
        ShouldSkip = shouldSkip;
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
    /// Sets new info to agv for the action.
    /// </summary>
    /// <param name="infoToAgv">The information for the agv.</param>
    public void SetInfoToAgv(Dictionary<InfoToAgvKey, string>? infoToAgv)
    {
           InfoToAgv = infoToAgv;
    }

    /// <summary>
    /// Sets a target for the action.
    /// </summary>
    /// <param name="targetNodeId">The target node id.</param>
    /// <param name="shouldValidate">If the action should validate that it has no target yet.</param>
    /// <exception cref="SetActionTargetFailedException">Thrown, if a target was already set previously.</exception>
    public void SetTarget(string targetNodeId, bool shouldValidate = true)
    {
        if (shouldValidate && Target.NodeId != string.Empty)
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
    /// Set this <see cref="AgvAction"/> to be skipped.
    /// </summary>
    public void Skip()
    {
        ShouldSkip = true;
    }

    /// <summary>
    /// Change the type of the action.
    /// </summary>
    /// <param name="newType">The new type of the action.</param>
    public void ChangeActiontype(ActionType newType)
    {
        ActionType = newType;
    }

    /// <summary>
    /// Set dock front of an action.
    /// </summary>
    /// <param name="needsRotation">True, if the pallet needs rotation.</param>
    /// <param name="previousDockFront">Value of dockFront of the previous action.</param>
    public void SetDockFront(bool needsRotation, bool? previousDockFront)
    {
        bool dockFront;
        switch (ActionType)
        {
            case ActionType.Pick:
            case ActionType.Move:
                dockFront = previousDockFront?? true;
                SetDockFrontValue(dockFront);
                return;

            case ActionType.Drop:
            {
                dockFront = needsRotation ? previousDockFront!.Value : !previousDockFront!.Value;
                if (ForceDropBackWards())
                {
                    dockFront = false;
                }
                SetDockFrontValue(dockFront);
                return;
            }

            case ActionType.Charge:
                SetDockFrontValue(true);
                return;
        }
    }

    private void SetDockFrontValue(bool dockFront)
    {
        InfoToAgv ??= new Dictionary<InfoToAgvKey, string>();
        InfoToAgv[InfoToAgvKey.DockFront] = dockFront.ToString();
    }

    private bool ForceDropBackWards()
    {
        return InfoToAgv != null && InfoToAgv.GetForceDroppingBackwards();
    }

    /// <summary>
    /// Simple string representation for logging.
    /// </summary>
    public override string ToString()
    {
        return $"Nodes: {string.Join(",", Nodes)} TargetNode: {Target.NodeId} CorrelationId: {CorrelationId}, ParentId: {ParentId} Type: {ActionType}, State {State}, ShouldSkip {ShouldSkip}";
    }

    /// <inheritdoc cref="IEquatable{T}"/>>
    public bool Equals(AgvAction? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (!CorrelationId.Equals(other.CorrelationId))
        {
            return false;
        }
        if (!ParentId.Equals(other.ParentId))
        {
            return false;
        }
        if (Sequence != other.Sequence)
        {
            return false;
        }
        if (State != other.State)
        {
            return false;
        }
        if (ActionType != other.ActionType)
        {
            return false;
        }
        if (!Target.Equals(other.Target))
        {
            return false;
        }
        if (!Nodes.Equals(other.Nodes))
        {
            return false;
        }
        if (!Edges.Equals(other.Edges))
        {
            return false;
        }
        if (!CreatedDate.Equals(other.CreatedDate))
        {
            return false;
        }
        if (!Nullable.Equals(StartedDate, other.StartedDate))
        {
            return false;
        }
        if (!Nullable.Equals(CompletedDate, other.CompletedDate))
        {
            return false;
        }
        if (!Nullable.Equals(PausedDate, other.PausedDate))
        {
            return false;
        }
        if (!Equals(InfoToAgv, other.InfoToAgv))
        {
            return false;
        }
        if (!PausedDuration.Equals(other.PausedDuration))
        {
            return false;
        }
        if (ShouldSkip != other.ShouldSkip)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc cref="IEquatable{T}"/>>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (obj.GetType() != GetType())
        {
            return false;
        }
        return Equals((AgvAction)obj);
    }

    /// <inheritdoc cref="IEquatable{T}"/>>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(value: CorrelationId);
        return hashCode.ToHashCode();
    }
}
