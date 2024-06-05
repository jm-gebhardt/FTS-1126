using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// Grouping actions and sending them across service modules
/// </summary>
public class ActionGroup
{
    private readonly IReadOnlyList<AgvAction> _actions = new List<AgvAction>();

    /// <summary>
    /// The parenting id.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// A unique ID of the order that clamps all the corresponding actions
    /// </summary>
    public string OrderId { get; init; }

    /// <summary>
    /// The id of the assigned agv that will execute the ActionGroup.
    /// </summary>
    public Guid? AssignedAgv { get; set; }

    /// <summary>
    /// The creation date of the action (UTC).
    /// </summary>
    public DateTime CreatedDate { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// The date the action has been started (UTC).
    /// </summary>
    public DateTime? StartedDate { get; set; }

    /// <summary>
    /// The date the action has been completed (UTC).
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// The state of the ActionGroup.
    /// </summary>
    public ActionGroupState State { get; set; } = ActionGroupState.Created;

    /// <summary>
    /// The first action to be executed.
    /// </summary>
    public AgvAction? FirstNotStartedAction => Actions.FirstOrDefault(a => a.State == ActionState.Created);

    /// <summary>
    /// True, if the action group is moving cargo.
    /// </summary>
    public bool IsMovingCargo => Actions.Any(a => a.ActionType == ActionType.Pick || a.ActionType == ActionType.Drop);

    /// <summary>
    /// True, if the action group is a charging order.
    /// </summary>
    public bool IsChargingOrder => Actions.Any(a => a.ActionType == ActionType.Charge);

    /// <summary>
    /// True if any of the actions is incomplete, i.e. its target is undefined.
    /// </summary>

    public bool IsIncomplete => Actions.Any(a => a.IsIncomplete);
    
    /// <summary>
    /// List of actions (ordered by sequence).
    /// </summary>
    public IReadOnlyList<AgvAction> Actions
    {
        get => _actions.OrderBy(a => a.Sequence).ToList();
        init => _actions = value;
    }

    /// <summary>
    /// If true, then this ActionGroup was created manually, not by the system.
    /// </summary>
    public bool IsManual { get; set; }

    /// <summary>
    /// The creator of the <see cref="ActionGroup"/>.
    /// </summary>
    public ActionGroupCreator Creator { get; init; }

    /// <summary>
    /// The estimated duration based on the estimated duration of the actions.
    /// </summary>
    public TimeSpan? EstimatedDuration { get; set; }

    /// <summary>
    /// If true, then this ActionGroup has priority over others (starts immediately).
    /// </summary>
    public bool HasPriority { get; init; }

    /// <summary>
    /// Are set when the ActionGroup went into a non successful state, like invalid, aborted or canceled. In case of invalid, this is considered as errors.
    /// </summary>
    [UsedImplicitly]
    public List<Reason> Reasons { get; set; } = new();

    /// <inheritdoc cref="ActionGroup"/>
    /// <param name="correlationId">The correlation id of the <see cref="ActionGroup"/>.</param>
    /// <param name="actions">The child actions of the <see cref="ActionGroup"/>.</param>
    /// <param name="orderId">The order id.</param>
    /// <param name="assignedAgv">The assigned agv of this <see cref="ActionGroup"/>.</param>
    /// <param name="creator">The creator of this <see cref="ActionGroup"/>.</param>
    /// <param name="createdDate">The date of the creation of this <see cref="ActionGroup"/>.</param>
    /// <param name="isManual">If true, then this <see cref="ActionGroup"/> was created manually, not by the system.</param>
    /// <param name="hasPriority">If true, then this <see cref="ActionGroup"/> has priority over others.</param>
    public ActionGroup(Guid correlationId, string orderId, IEnumerable<AgvAction> actions, ActionGroupCreator creator, DateTime? createdDate = default, Guid? assignedAgv = null, bool isManual = false, bool hasPriority=false) : this(correlationId, orderId)
    {
        AssignedAgv = assignedAgv;
        IsManual = isManual;
        HasPriority = hasPriority;
        CreatedDate = createdDate.GetValueOrDefault(DateTime.UtcNow);
        Actions = actions.ToList();
        Creator = creator;
    }

    /// <summary>
    /// Initializes a new <see cref="ActionGroup"/> instance.
    /// </summary>
    /// <param name="correlationId">The correlation id of the ActionGroup.</param>
    /// <param name="orderId">The order id.</param>
    public ActionGroup(Guid correlationId, string orderId)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
    }

    /// <summary>
    /// Json ctor
    /// </summary>
    public ActionGroup()
    {
        OrderId = "";
    }

    /// <summary>
    /// Updates the state of the ActionGroup based on the state of its actions.
    /// </summary>
    public void UpdateActionGroupState()
    {
        var previousState = State;

        if (!Actions.Any())
        {
            throw new ActionsInActionGroupEmptyException(CorrelationId);
        }

        State = Actions switch
        {
            _ when Actions.All(a => a.State == ActionState.Completed) => ActionGroupState.Completed,
            _ when Actions.All(a => a.State == ActionState.Created) => ActionGroupState.Created,
            _ when Actions.Any(a => a.State == ActionState.Failed) => ActionGroupState.Failed,
            _ when Actions.Any(a => a.State == ActionState.Canceled) => ActionGroupState.Canceled,
            _ when Actions.Any(a => a.State == ActionState.Aborted) => ActionGroupState.Aborted,
            _ when Actions.Any(a => a.State == ActionState.Invalid) => ActionGroupState.Invalid,
            _ => ActionGroupState.Started
        };

        if (State != previousState)
        {
            SetDates();
        }
    }

    private void SetDates()
    {
        var now = DateTime.UtcNow;
        switch (State)
        {
            case ActionGroupState.Started:
                StartedDate = now;
                break;

            case ActionGroupState.Completed:
                CompletedDate = now;
                break;

            case ActionGroupState.Failed:
            case ActionGroupState.Canceled:
            case ActionGroupState.Invalid:
                CompletedDate = now;
                break;

            case ActionGroupState.Aborted:
                CompletedDate = now;
                break;
        }
    }

    /// <summary>
    /// Sets the AssignedAgv of ActionGroup if not already assigned.
    /// </summary>
    /// <param name="agvToAssign">The agv to assign.</param>
    /// <exception cref="ActionGroupAlreadyAssignedException">If the action group was already assigned.</exception>
    public void Assign(Agv.Agv agvToAssign)
    {
        if (AssignedAgv == null)
        {
            AssignedAgv = agvToAssign.CorrelationId;
        }
        else if (AssignedAgv != agvToAssign.CorrelationId)
        {
            throw new ActionGroupAlreadyAssignedException(this, agvToAssign.CorrelationId);
        }
    }

    /// <summary>
    /// Completes the specified action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    public void CompleteAction(Guid actionId)
    {
        UpdateActionState(actionId, ActionState.Completed);
    }

    /// <summary>
    /// Invalidates the specified action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    public void InvalidateAction(Guid actionId)
    {
        UpdateActionState(actionId, ActionState.Invalid);
    }

    /// <summary>
    /// Aborts the specified action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    /// <param name="reasons">The reasons for the abort.</param>
    public void AbortAction(Guid actionId, IEnumerable<Reason> reasons)
    {
        Reasons.AddRange(reasons);
        UpdateActionState(actionId, ActionState.Aborted);
    }

    /// <summary>
    /// Sets the specified action into the failed state.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    public void SetActionFailed(Guid actionId)
    {
        UpdateActionState(actionId, ActionState.Failed);
    }

    /// <summary>
    /// Cancels the specified action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    public void CancelAction(Guid actionId)
    {
        UpdateActionState(actionId, ActionState.Canceled);
    }

    /// <summary>
    /// Reset the state of the action to <see cref="ActionState.Created"/>.
    /// </summary>
    /// <param name="actionId">The action id.</param>
    private void ResetAction(Guid actionId)
    {
        var action = Actions.Single(a => a.CorrelationId == actionId);
        action.StartedDate = null;
        action.CompletedDate = null;
        action.Nodes = Array.Empty<string>();
        EstimatedDuration = TimeSpan.Zero;
        action.UpdateActionState(ActionState.Created);
        UpdateActionGroupState();
    }

    /// <summary>
    /// Starts the specified action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    /// <param name="nodes">The nodes of the action.</param>
    public void StartAction(Guid actionId, string[] nodes)
    {
        Actions.Single(a => a.CorrelationId == actionId).Nodes = nodes;
        UpdateActionState(actionId, ActionState.Started);
    }

    /// <summary>
    /// Set a target for an action.
    /// </summary>
    /// <param name="actionId">The correlation id of the action.</param>
    /// <param name="targetNode">The target node.</param>
    /// <exception cref="ActionNotFoundException">Thrown if no action with was found using the action id</exception>
    /// <exception cref="SetActionTargetFailedException">Thrown if the action already had a target</exception>
    public void SetTargetForAction(Guid actionId, string targetNode)
    {
        if (Actions.All(a => a.CorrelationId != actionId))
        {
            throw new ActionNotFoundException("Action not found using id", "CorrelationId", actionId);
        }
        Actions.Single(a => a.CorrelationId == actionId).SetTarget(targetNode);
    }

    private void UpdateActionState(Guid actionId, ActionState state)
    {
        Actions.Single(a => a.CorrelationId == actionId).UpdateActionState(state);
        UpdateActionGroupState();
    }

    /// <summary>
    /// Cancels the <see cref="ActionGroup"/> and all the child <see cref="AgvAction"/>.
    /// </summary>
    /// <returns></returns>
    public bool CancelActionGroup()
    {
        if (Actions.Any(a => a.State != ActionState.Created))
        {
            return false;
        }
        foreach (var agvAction in Actions)
        {
            CancelAction(agvAction.CorrelationId);
        }
        return true;
    }

    /// <summary>
    /// Resets the <see cref="ActionGroup"/> and all <see cref="AgvAction"/> to
    /// <see cref="ActionState.Created"/>.
    /// </summary>
    public void ResetActionGroup()
    {
        foreach (var agvAction in Actions)
        {
            ResetAction(agvAction.CorrelationId);
        }
        AssignedAgv = null;
        StartedDate = null;
        CompletedDate = null;
    }

    /// <summary>
    /// Cancels the <see cref="ActionGroup"/> and all the child <see cref="AgvAction"/>.
    /// </summary>
    /// <param name="reasons"></param>
    /// <returns></returns>
    public void InvalidateActionGroup(IEnumerable<Reason> reasons)
    {
        Reasons.AddRange(reasons);
        foreach (var agvAction in Actions)
        {
            InvalidateAction(agvAction.CorrelationId);
        }
    }

    /// <summary>
    /// Get the targets of the action group.
    /// </summary>
    /// <returns>Node ids if the targets.</returns>
    public string[] GetTargets()
    {
        return !Actions.Any() ? [] : Actions
            .Where((action, index) => !action.IsIncomplete &&
                                    (index == 0 || (!Actions[index - 1].IsIncomplete && action.Sequence == Actions[index - 1].Sequence + 1)))
            .Select(action => action.TargetNodeId)
            .ToArray();
    }

    /// <summary>
    /// Set dates from given duration values (for unit testing).
    /// </summary>
    /// <param name="actionDuration">The duration of each action [s].</param>
    /// <param name="startDate">The start date.</param>
    /// <param name="idleTimeBetweenActions">The idle time between actions [s].</param>
    public void SetFixedDatesFromDurations(double actionDuration, DateTime startDate, double idleTimeBetweenActions)
    {
        StartedDate = startDate;
        foreach (var action in Actions)
        {
            action.StartedDate = startDate;
            action.CompletedDate = startDate.AddSeconds(actionDuration);
            startDate = action.CompletedDate!.Value.AddSeconds(idleTimeBetweenActions);
        }
        CompletedDate = Actions[^1].CompletedDate;
    }
}