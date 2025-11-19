using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.TransportOrder.Domain.Extensions;
using JetBrains.Annotations;
using System.Globalization;
using System.Text.Json.Serialization;

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
    public ActionType ActionType { get; set; }

    /// <summary>
    /// The target of the action.
    /// </summary>
    public ActionTarget Target { get; set; } = null!;

    /// <summary>
    /// True, if the action is heading towards a position given
    /// by coordinates instead of node id.
    /// </summary>
    public bool IsMoveToCoordinate => Target.Node?.Id == null && Target.Position != null;

    /// <summary>
    /// The nodes of the trip.
    /// </summary>
    public GraphNode[] Nodes { get; set; } = [];

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
    /// Additional information for this action, often used for external systems.
    /// </summary>
    public string? AdditionalInfo { get; set; }

    /// <summary>
    /// Duration that the action has been paused for and run previously in seconds
    /// </summary>
    public long PriorRunAndPauseDuration { get; set; }

    /// <summary>
    /// The duration of the action.
    /// </summary>
    public TimeSpan? Duration => CompletedDate - StartedDate + TimeSpan.FromSeconds(PriorRunAndPauseDuration);

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
    public bool IsIncomplete => Target.Node?.Id == null && Target.Position == null;

    /// <summary>
    /// True, if Dockfront has been set externally.
    /// </summary>
    public bool HasDockFront()
    {
        if (InfoToAgv == null)
        {
            return false;
        }

        return InfoToAgv.TryGetValue(InfoToAgvKey.DockFront, out string? value) && bool.TryParse(value, out _);
    }

    /// <summary>
    /// True, if the lift height has been set externally.
    /// </summary>
    public bool HasLiftHeight()
    {
        if (InfoToAgv == null)
        {
            return false;
        }

        return InfoToAgv.TryGetValue(InfoToAgvKey.LiftHeight, out string? value) && double.TryParse(value, out _);
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="correlationId">Unique correlation ID of this action (see order action repository)</param>
    /// <param name="actionType">The type of the action, e.g. Pick or Drop</param>
    /// <param name="targetNode">Target node of the action</param>
    /// <param name="parentId">The CorrelationId of the ActionGroup where this action belongs to</param>
    /// <param name="infoToAgv">Information that will be passed to the agv.</param>
    public AgvAction(Guid correlationId, ActionType actionType, GraphNode? targetNode, Guid parentId, Dictionary<InfoToAgvKey, string>? infoToAgv = null)
    {
        CorrelationId = correlationId;
        ActionType = actionType;
        Target = new ActionTarget(targetNode);
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
        GraphNode[] nodes,
        Guid[] edges,
        DateTime createdDate,
        DateTime? startedDate,
        DateTime? completedDate,
        DateTime? pausedDate,
        Dictionary<InfoToAgvKey, string>? infoToAgv,
        long priorRunAndPauseDuration,
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
        PriorRunAndPauseDuration = priorRunAndPauseDuration;
        ShouldSkip = shouldSkip;
    }

    /// <summary>
    /// Updates the state of the Action and its Dates
    /// </summary>
    /// <param name="state">The new state.</param>
    /// <param name="now">The current time.</param>
    public void UpdateActionState(ActionState state, DateTimeOffset now)
    {
        if (State == state)
        {
            return;
        }

        if (State == ActionState.Paused && PausedDate != null)
        {
            var pauseTimeSpan = (TimeSpan) (now.UtcDateTime - PausedDate);
            var previousRunTime = (TimeSpan) (PausedDate - StartedDate.GetValueOrDefault());
            PriorRunAndPauseDuration += (long) pauseTimeSpan.TotalSeconds + (long) previousRunTime.TotalSeconds;

            PausedDate = null;
        }

        State = state;

        SetDates(now);
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
    /// <param name="targetNode">The target node</param>
    public void SetTarget(GraphNode targetNode)
    {
        // this is needed for EF
        Target.Node = new GraphNode(targetNode.Id, targetNode.Name);
    }

    private void SetDates(DateTimeOffset now)
    {
        switch (State)
        {
            case ActionState.Started:
                StartedDate = now.UtcDateTime;
                break;

            case ActionState.Completed:
                CompletedDate = now.UtcDateTime;
                break;

            case ActionState.Aborted:
            case ActionState.Canceled:
            case ActionState.Invalid:
                CompletedDate = now.UtcDateTime;
                break;

            case ActionState.Paused:
                PausedDate = now.UtcDateTime;
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
    /// <param name="palletNeedsRotation">True, if the pallet needs rotation.</param>
    /// <param name="previousDockFront">Value of dockFront of the previous action.</param>
    /// <param name="nodeDockingDirection">The docking direction as given by the node.</param>
    /// <param name="noTurning">True, if the agv should not turn for a followup action.</param>
    public void SetDockFront(bool palletNeedsRotation, bool? previousDockFront, DockingDirection? nodeDockingDirection, bool noTurning = false)
    {
        bool dockFront;

        bool isDoppelspiel = Nodes.Length == 1 && ActionType == ActionType.Pick;
        if ((IsFirstAction && isDoppelspiel) || ActionType == ActionType.Charge || (IsFirstAction && noTurning))
        {
            dockFront = previousDockFront ?? true; // Fallback to avoid exceptions
            SetDockFrontValue(nodeDockingDirection.GetDockfront(dockFront));
            return;
        }

        if (IsFirstAction)
        {
            dockFront = previousDockFront == null || !previousDockFront.Value;
            SetDockFrontValue(nodeDockingDirection.GetDockfront(dockFront));
            return;
        }

        if (noTurning)
        {
            dockFront = previousDockFront!.Value;
            if (ActionType == ActionType.Drop)
            {
                // only if rotation is needed explicitly
                dockFront = palletNeedsRotation ? !dockFront : dockFront;
                if (ForceDropBackWards())
                {
                    dockFront = false;
                }
            }
            SetDockFrontValue(nodeDockingDirection.GetDockfront(dockFront));
            return;
        }

        dockFront = !previousDockFront!.Value;
        if (ActionType == ActionType.Drop)
        {
            dockFront = palletNeedsRotation ? !dockFront : dockFront;
            if (ForceDropBackWards())
            {
                dockFront = false;
            }
        }
        SetDockFrontValue(nodeDockingDirection.GetDockfront(dockFront));
    }

    /// <summary>
    /// Sets the dock front of this Action
    /// </summary>
    /// <param name="dockFront">What to set the dock front to.</param>
    public void SetDockFrontValue(bool dockFront)
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
        return $"Nodes: {string.Join(",", Nodes.Select(n => n.Name))} TargetNode: {Target.Node?.Name} CorrelationId: {CorrelationId}, ParentId: {ParentId} Type: {ActionType}, State {State}, ShouldSkip {ShouldSkip}";
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
        if (!PriorRunAndPauseDuration.Equals(other.PriorRunAndPauseDuration))
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
        return Equals((AgvAction) obj);
    }

    /// <inheritdoc cref="IEquatable{T}"/>>
    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(value: CorrelationId);
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Gets the SAP information from the additional action information.
    /// </summary>
    /// <returns>The value of SAP.</returns>
    public string? GetSap()
    {
        return AdditionalInfo.ExtractValue("SAP", ";");
    }

    /// <summary>
    /// Set the lift height based on additional action information.
    /// </summary>
    public void SetExternalLiftHeight()
    {
        string? liftHeight = AdditionalInfo.ExtractValue("LiftHeight", ";");
        if (liftHeight == null)
        {
            return;
        }

        InfoToAgv ??= new Dictionary<InfoToAgvKey, string>();
        InfoToAgv[InfoToAgvKey.LiftHeight] = liftHeight;
    }

    /// <summary>
    /// Set the lift height.
    /// </summary>
    public void SetLiftHeight(double? liftHeight)
    {
        if (liftHeight == null)
        {
            if (HasLiftHeight())
            {
                InfoToAgv!.Remove(InfoToAgvKey.LiftHeight);
            }
            return;
        }

        if (HasLiftHeight())
        {
            return;
        }

        InfoToAgv ??= new Dictionary<InfoToAgvKey, string>();
        InfoToAgv.Add(InfoToAgvKey.LiftHeight, liftHeight.Value.ToString(CultureInfo.InvariantCulture));
    }
}
