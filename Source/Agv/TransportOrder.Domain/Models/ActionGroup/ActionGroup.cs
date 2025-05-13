using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Extensions;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;

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
    public AgvAction? FirstNotStartedAction => Actions.FirstOrDefault(a => a is { State: ActionState.Created, ShouldSkip: false });

    /// <summary>
    /// The first action that is not started
    /// </summary>
    public AgvAction? FirstNotCompletedAction => Actions.FirstOrDefault(a => a.State is ActionState.Started or ActionState.Created or ActionState.Paused && !a.ShouldSkip);

    /// <summary>
    /// True, if the action group is moving cargo.
    /// </summary>
    public bool IsMovingCargo => Actions.Any(a => a.ActionType is ActionType.Pick or ActionType.Drop);

    /// <summary>
    /// True, if the action group is a charging order.
    /// </summary>
    public bool IsChargingOrder => Actions.Any(a => a.ActionType == ActionType.Charge);

    /// <summary>
    /// True if any of the actions is incomplete, i.e. its target is undefined.
    /// </summary>

    public bool IsIncomplete => Actions.Any(a => a.IsIncomplete);

    /// <summary>
    /// True, if the action group is queued.
    /// </summary>
    public bool IsQueued { get; private set; }

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
    public bool HasPriority { get; set; }

    /// <summary>
    /// The handling unit corresponding to this ActionGroup.
    /// </summary>
    public string? HandlingUnit { get; init; }

    /// <summary>
    /// Are set when the ActionGroup went into a not successful state, like invalid, aborted or canceled. In case of invalid, this is considered as errors.
    /// </summary>
    [UsedImplicitly]
    public List<Reason> Reasons { get; set; } = new();

    /// <inheritdoc cref="ActionGroup"/>
    public ActionGroup(Guid correlationId, string orderId, IEnumerable<AgvAction> actions, ActionGroupCreator creator, bool isQueued = false, DateTime? createdDate = null, Guid? assignedAgv = null, bool isManual = false, bool hasPriority = false) : this(correlationId, orderId)
    {
        AssignedAgv = assignedAgv;
        IsManual = isManual;
        IsQueued = isQueued;
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
    /// Queue the action group.
    /// </summary>
    public void Queue()
    {
        IsQueued = true;
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
            _ when Actions.All(a => a.State == ActionState.Completed || a.ShouldSkip) => ActionGroupState.Completed,
            _ when Actions.All(a => a.State == ActionState.Created || a.ShouldSkip) => ActionGroupState.Created,
            _ when Actions.Any(a => a.State == ActionState.Failed) => ActionGroupState.Failed,
            _ when Actions.Any(a => a.State == ActionState.Canceled) => ActionGroupState.Canceled,
            _ when Actions.Any(a => a.State == ActionState.Aborted) => ActionGroupState.Aborted,
            _ when Actions.Any(a => a.State == ActionState.Invalid) => ActionGroupState.Invalid,
            _ when Actions.Any(a => a.State == ActionState.Paused) => ActionGroupState.Paused,
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
        if (State == ActionGroupState.Paused && Actions[^1].CorrelationId != actionId)
        {
            var actionToPause = Actions[Actions.ToList().FindIndex(a => a.CorrelationId == actionId) + 1];
            UpdateActionState(actionToPause.CorrelationId, ActionState.Paused);
        }

        UpdateActionState(actionId, ActionState.Completed);
    }

    /// <summary>
    /// Pauses the currently started action of the action group.
    /// Alternatively pauses the first action that is in created if none is started.
    /// </summary>
    /// <param name="reason">Pause reason.</param>
    public void PauseActionGroup(Reason? reason)
    {
        var action = FirstNotCompletedAction;
        if (action == null)
        {
            return;
        }

        if (reason != null)
        {
            // For each station that is locked, add a reason.
            // If agv mode is changed (locked, maintenance, user) add reason only once.
            // Paused action will be resumed when all reasons are reset in ResumeActionGroup.
            var otherPauseReason = Reasons.SingleOrDefault(r => r.Template != Constants.Reasons.LockedStation);
            switch (reason.IsPauseReasonLockedStation())
            {
                case true:
                case false when otherPauseReason == null:
                    Reasons.Add(reason);
                    break;
            }
        }

        UpdateActionState(action.CorrelationId, ActionState.Paused);
    }

    /// <summary>
    /// Resumes the paused action of this action group.
    /// </summary>
    /// <param name="removeStationReason">True: Remove lock station reason.</param>
    public void ResumeActionGroup(bool removeStationReason)
    {
        var action = Actions.FirstOrDefault(a => a.State == ActionState.Paused);
        if (action == null)
        {
            return;
        }

        var lockStationReason = Reasons.FirstOrDefault(r => r.Template == Constants.Reasons.LockedStation);
        var otherPauseReason = Reasons.SingleOrDefault(r => r.Template != Constants.Reasons.LockedStation);
        switch (removeStationReason)
        {
            case true when lockStationReason != null:
                Reasons.Remove(lockStationReason);
                break;

            case false when otherPauseReason != null:
                Reasons.Remove(otherPauseReason);
                break;
        }

        if (Reasons.IsNullOrEmpty())
        {
            UpdateActionState(action.CorrelationId, ActionState.Created);
            HasPriority = true;
        }
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
    /// <param name="action">The action.</param>
    private void ResetAction(AgvAction action)
    {
        action.StartedDate = null;
        action.CompletedDate = null;
        action.Nodes = [];
        action.Edges = [];
        EstimatedDuration = TimeSpan.Zero;
        action.UpdateActionState(ActionState.Created);
        UpdateActionGroupState();
    }

    /// <summary>
    /// Starts the specified action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    /// <param name="nodes">The nodes of the action.</param>
    /// <param name="edges">The edges of the action.</param>
    public void StartAction(Guid actionId, string[] nodes, Guid[] edges)
    {
        Actions.Single(a => a.CorrelationId == actionId).Nodes = nodes;
        Actions.Single(a => a.CorrelationId == actionId).Edges = edges;
        UpdateActionState(actionId, ActionState.Started);
    }

    /// <summary>
    /// Set the dock front information for an action.
    /// </summary>
    /// <param name="actionId">The id of the action for which dockfront is to be set.</param>
    /// <param name="dockFrontPreviousActionGroup">The value of dockfront of the last action of the previous action group.</param>
    public void SetDockFrontOfAction(Guid actionId, bool? dockFrontPreviousActionGroup)
    {
        var action = Actions.Single(a => a.CorrelationId == actionId);

        if (IsManual && action.HasDockFront())
        {
            return;
        }

        bool needsRotation = GetNeedsRotation();

        if (IsChargingOrder)
        {
            action.SetDockFront(needsRotation, null);
            return;
        }

        bool isAutomaticParkOrder = Actions.All(a => a.ActionType == ActionType.Move) && Actions.Count == 1 && !IsManual && !HasPriority;
        if (isAutomaticParkOrder)
        {
            action.SetDockFront(false, null);
            return;
        }

        if (action.IsFirstAction)
        {
            action.SetDockFront(needsRotation, dockFrontPreviousActionGroup);
            return;
        }
        var previousAction = Actions.Single(a => a.Sequence == action.Sequence - 1);
        action.SetDockFront(needsRotation, previousAction.InfoToAgv!.GetDockFront());
    }

    private bool GetNeedsRotation()
    {
        var pickAction = Actions.SingleOrDefault(a => a is { ActionType: ActionType.Pick, ShouldSkip: false });
        return pickAction?.InfoToAgv != null && pickAction.InfoToAgv.NeedsRotation();
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
            ResetAction(agvAction);
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
        return !Actions.Any() ?
            [] :
            Actions
                .Where((action, index) => !action.IsIncomplete &&
                                          (index == 0 || (!Actions[index - 1].IsIncomplete && action.Sequence == Actions[index - 1].Sequence + 1)))
                .Select(action => action.Target.NodeId!)
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

    /// <summary>
    /// Skips the provided action and updates the previous one with its type and the new target.
    /// </summary>
    /// <param name="actionId">The action id to skip.</param>
    /// <param name="targetNodeId">The id of the new target node.</param>
    /// <param name="route">The route, if any.</param>
    public AgvAction SetTargetForActionWithUpdate(Guid actionId, string targetNodeId, Route? route = null)
    {
        if (Actions.All(a => a.CorrelationId != actionId))
        {
            throw new ActionNotFoundException("Action not found using id", "CorrelationId", actionId);
        }

        var toSkip = Actions.First(a => a.CorrelationId == actionId);
        var toUpdate = Actions.OrderBy(a => a.Sequence).Last(a => a.Sequence < toSkip.Sequence);
        toUpdate.ChangeActiontype(toSkip.ActionType);
        toUpdate.SetTarget(targetNodeId, false);
        if (toSkip.ActionType == ActionType.Pick)
        {
            toUpdate.SetInfoToAgv(toSkip.InfoToAgv);
        }
        if (route != null)
        {
            toUpdate.Nodes = route.Nodes;
            toUpdate.Edges = route.Edges;
        }
        toSkip.Skip();
        return toUpdate;
    }

    /// <summary>
    /// Gets the previous action if it exists.
    /// </summary>
    /// <param name="actionId">Action to find the previous for.</param>
    public AgvAction? GetPreviousAction(Guid actionId)
    {
        if (Actions.All(a => a.CorrelationId != actionId))
        {
            throw new ActionNotFoundException("Action not found using id", "CorrelationId", actionId);
        }
        int sequence = Actions.First(a => a.CorrelationId == actionId).Sequence;

        return Actions.OrderBy(a => a.Sequence).LastOrDefault(a => a.Sequence < sequence);
    }

    /// <summary>
    /// Gets the first action that is not skipped after the provided action.
    /// </summary>
    /// <param name="actionId">Tha action to start at</param>
    public AgvAction? GetNextNotSkippedAction(Guid actionId)
    {
        if (Actions.All(a => a.CorrelationId != actionId))
        {
            throw new ActionNotFoundException("Action not found using id", "CorrelationId", actionId);
        }
        var action = Actions.First(a => a.CorrelationId == actionId);

        return Actions.OrderBy(a => a.Sequence).FirstOrDefault(a => a.Sequence > action.Sequence && !a.ShouldSkip);
    }

    /// <summary>
    /// True if the action group is outdated according to the expiration time.
    /// </summary>
    /// <param name="expirationTime">Time span of expiration.</param>
    /// <param name="now">Current time stamp.</param>
    /// <returns>True if the started or created date are prior to the expiration time.</returns>
    public bool IsOutdated(TimeSpan expirationTime, DateTime now)
    {
        var expirationDate = now.Add(-expirationTime);
        if (StartedDate < expirationDate)
        {
            return true;
        }
        return CreatedDate < expirationDate;
    }
}