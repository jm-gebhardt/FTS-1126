using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Extensions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using Gebhardt.Acc.TransportOrder.Routing.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;

/// <summary>
/// Grouping actions and sending them across service modules
/// </summary>
public class ActionGroup
{
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
    /// Whether this action group is only relevant in the current system context.
    /// Action groups with this flag will be canceled/aborted in certain mode switches
    /// and should be of a type to be recreated automatically when the mode is switched back if they are still relevant.
    /// E.g. Queue entry action groups will be recreated when a queue is still under capacity.
    /// </summary>
    public bool DoNotPause { get; }

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
    /// True, if the action group is a queueing order.
    /// </summary>
    public bool IsQueueingOrder => Priority == Priority.Queueing && !IsManual && Actions.All(a => a.ActionType == ActionType.Move) && Actions.Count == 1;

    /// <summary>
    /// True if any of the actions is incomplete, i.e. its target is undefined.
    /// </summary>

    public bool IsIncomplete => Actions.Any(a => a.IsIncomplete);

    /// <summary>
    /// True, if the action group is queued.
    /// </summary>
    public bool IsQueued { get; private set; }

    /// <summary>
    /// The priority of the action group.
    /// </summary>
    public Priority Priority { get; set; }

    /// <summary>
    /// If true, then this ActionGroup allows to trigger escaping from the queue
    /// </summary>
    public bool ShouldEscapeQueue { get; init; }

    /// <summary>
    /// List of actions (ordered by sequence).
    /// </summary>
    public IReadOnlyList<AgvAction> Actions
    {
        get => field.OrderBy(a => a.Sequence).ToList();
        init;
    } = new List<AgvAction>();

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
    /// The handling unit corresponding to this ActionGroup.
    /// </summary>
    public string? HandlingUnit { get; init; }

    /// <summary>
    /// Duration of the action group.
    /// </summary>
    public TimeSpan? Duration => CompletedDate - StartedDate;

    /// <summary>
    /// Are set when the ActionGroup went into a not successful state, like invalid, aborted or canceled. In case of invalid, this is considered as errors.
    /// </summary>
    [UsedImplicitly]
    public List<Reason> Reasons { get; set; } = new();

    /// <inheritdoc cref="ActionGroup"/>
    public ActionGroup(Guid correlationId, string orderId, IEnumerable<AgvAction> actions, ActionGroupCreator creator, Priority priority = Priority.Default, bool shouldEscapeQueue = false, bool isQueued = false, DateTime? createdDate = null, Guid? assignedAgv = null, bool isManual = false, bool doNotPause = false) : this(correlationId, orderId)
    {
        AssignedAgv = assignedAgv;
        IsManual = isManual;
        IsQueued = isQueued;
        Priority = priority;
        CreatedDate = createdDate.GetValueOrDefault(DateTime.UtcNow);
        Actions = actions.ToList();
        Creator = creator;
        DoNotPause = doNotPause;
        ShouldEscapeQueue = shouldEscapeQueue;
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
    /// Retract the action group.
    /// </summary>
    public void Retract()
    {
        IsQueued = false;
        AssignedAgv = null;
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
                StartedDate ??= Actions.OrderBy(a => a.Sequence).FirstOrDefault(a => a.StartedDate != null)?.StartedDate ?? now;
                break;

            case ActionGroupState.Completed:
                CompletedDate ??= Actions.Where(a => a.CompletedDate != null).MaxBy(a => a.CompletedDate)?.CompletedDate ?? now;
                break;

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
    /// <param name="now">The current time.</param>
    public void CompleteAction(Guid actionId, DateTimeOffset now)
    {
        if (State == ActionGroupState.Paused && Actions[^1].CorrelationId != actionId)
        {
            var actionToPause = Actions[Actions.ToList().FindIndex(a => a.CorrelationId == actionId) + 1];
            UpdateActionState(actionToPause.CorrelationId, ActionState.Paused, now);
        }

        UpdateActionState(actionId, ActionState.Completed, now);
    }

    /// <summary>
    /// Pauses the currently started action of the action group.
    /// Alternatively pauses the first action that is in created if none is started.
    /// </summary>
    /// <param name="reason">Pause reason.</param>
    /// <param name="now">The current time.</param>
    public void PauseActionGroup(Reason? reason, DateTimeOffset now)
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

        UpdateActionState(action.CorrelationId, ActionState.Paused, now);
    }

    /// <summary>
    /// Resumes the paused action of this action group.
    /// </summary>
    /// <param name="removeStationReason">True: Remove lock station reason.</param>
    /// <param name="now">The current time.</param>
    public void ResumeActionGroup(bool removeStationReason, DateTimeOffset now)
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
            UpdateActionState(action.CorrelationId, ActionState.Created, now);
            if (Priority == Priority.Default)
            {
                Priority = Priority.Resume;
            }
        }
    }

    /// <summary>
    /// Invalidates the specified action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    /// <param name="now">The current time.</param>
    public void InvalidateAction(Guid actionId, DateTimeOffset now)
    {
        UpdateActionState(actionId, ActionState.Invalid, now);
    }

    /// <summary>
    /// Aborts the specified action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    /// <param name="reasons">The reasons for the abort.</param>
    /// <param name="now">The current time.</param>
    public void AbortAction(Guid actionId, IEnumerable<Reason> reasons, DateTimeOffset now)
    {
        Reasons.AddRange(reasons);
        UpdateActionState(actionId, ActionState.Aborted, now);
    }

    /// <summary>
    /// Sets the specified action into the failed state.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    /// <param name="now">The current time.</param>
    public void SetActionFailed(Guid actionId, DateTimeOffset now)
    {
        UpdateActionState(actionId, ActionState.Failed, now);
    } 

    /// <summary>
    /// Fail and diagnose reason.
    /// </summary>
    /// <param name="actionId">The correlation id of the action to fail.</param>
    /// <param name="targetIsLocked">True, if the target station is locked.</param>
    public string Fail(Guid actionId, bool targetIsLocked)
    {
        var action = Actions.Single(a => a.CorrelationId == actionId);
        var targetNode = action.Target.Node;
        SetActionFailed(actionId, DateTimeOffset.UtcNow);
        if (targetNode != null)
        {
            if (targetIsLocked)
            {
                Reasons.Add(FailReason.LockedStation.ToReason()!);
                return Constants.Reasons.ActionFailedLocked;
            }

            switch (action.ActionType)
            {
                case ActionType.Pick when !targetNode.IsPickStation():
                case ActionType.Drop when !targetNode.IsDropStation():
                case ActionType.Charge when !targetNode.IsCharger():
                    Reasons.Add(FailReason.WrongStation.ToReason()!);
                    return Constants.Reasons.ActionFailedWrongTargetType;
            }
        }
        Reasons.Add(FailReason.None.ToReason()!);
        return Constants.Reasons.ActionFailedForUnknownReason;
    }

    /// <summary>
    /// Cancels the specified action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    /// <param name="now">The current time.</param>
    public void CancelAction(Guid actionId, DateTimeOffset now)
    {
        UpdateActionState(actionId, ActionState.Canceled, now);
    }

    /// <summary>
    /// Reset the state of the action to <see cref="ActionState.Created"/>.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <param name="now">The current time.</param>
    private void ResetAction(AgvAction action, DateTimeOffset now)
    {
        action.StartedDate = null;
        action.CompletedDate = null;
        action.PausedDate = null;
        action.Nodes = [];
        action.Edges = [];
        action.PriorRunAndPauseDuration = 0;
        EstimatedDuration = TimeSpan.Zero;
        action.UpdateActionState(ActionState.Created, now);
        UpdateActionGroupState();
    }

    /// <summary>
    /// Starts the specified action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    /// <param name="nodes">The nodes of the action.</param>
    /// <param name="edges">The edges of the action.</param>
    /// <param name="now">The current time.</param>
    public void StartAction(Guid actionId, GraphNode[] nodes, Guid[] edges, DateTimeOffset now)
    {
        Actions.Single(a => a.CorrelationId == actionId).Nodes = nodes;
        Actions.Single(a => a.CorrelationId == actionId).Edges = edges;
        UpdateActionState(actionId, ActionState.Started, now);
    }

    /// <summary>
    /// Set the dock front information for an action.
    /// </summary>
    /// <param name="actionId">The id of the action for which DockFront is to be set.</param>
    /// <param name="dockFrontPreviousActionGroup">The value of DockFront of the last action of the previous action group.</param>
    /// <param name="nodeDockingDirection">The docking direction as given by the node.</param>
    /// <param name="startIsStation">True, if the start node is a station.</param>
    public void SetDockFrontOfAction(Guid actionId, bool? dockFrontPreviousActionGroup, DockingDirection? nodeDockingDirection, bool startIsStation = false)
    {
        var action = Actions.Single(a => a.CorrelationId == actionId);

        // Do not reset dock front, if it has been set manually
        if (IsManual && action.HasDockFront())
        {
            return;
        }
        bool noTurning;
        bool isMove = Actions.All(a => a.ActionType == ActionType.Move) && Actions.Count == 1;
        if (isMove)
        {
            noTurning = (Priority == Priority.QueueEvacuation && !startIsStation) || (Priority == Priority.Queueing && !startIsStation) || (IsManual && !startIsStation);
            action.SetDockFront(false, dockFrontPreviousActionGroup, nodeDockingDirection, noTurning);
            return;
        }

        if (IsChargingOrder)
        {
            action.SetDockFront(false, null, nodeDockingDirection);
            return;
        }

        bool palletNeedsRotation = GetNeedsRotation();
        if (action.IsFirstAction)
        {
            action.SetDockFront(palletNeedsRotation, dockFrontPreviousActionGroup, nodeDockingDirection, !startIsStation);
            return;
        }
        var previousAction = Actions.OrderBy(a => a.Sequence).Last(a => !a.ShouldSkip && a.Sequence < action.Sequence);
        noTurning = previousAction.ActionType == ActionType.Move;
        action.SetDockFront(palletNeedsRotation, previousAction.InfoToAgv!.GetDockFront(), nodeDockingDirection, noTurning);
    }

    private bool GetNeedsRotation()
    {
        var pickAction = Actions.SingleOrDefault(a => a is { ActionType: ActionType.Pick, ShouldSkip: false });
        return pickAction?.InfoToAgv != null && pickAction.InfoToAgv.NeedsRotation();
    }

    /// <summary>
    /// Set the lift height for an action.
    /// </summary>
    /// <param name="actionId">The id of the action.</param>
    /// <param name="pickLiftHeight">The picking lift height.</param>
    /// <param name="dropLiftHeight">The dropping lift height.</param>
    public void SetCargoLiftHeightForAction(Guid actionId, double? pickLiftHeight, double? dropLiftHeight)
    {
        var action = Actions.Single(a => a.CorrelationId == actionId);
        if (action.HasLiftHeight() && IsManual)
        {
            return;
        }

        if (action.ActionType == ActionType.Pick)
        {
            action.SetLiftHeight(pickLiftHeight);
        }

        if (action.ActionType == ActionType.Drop)
        {
            action.SetLiftHeight(dropLiftHeight);
        }

        double? liftHeight = ((double?[])[pickLiftHeight, dropLiftHeight]).FirstOrDefault(l => l != null);
        action.SetLiftHeight(liftHeight);
    }

    /// <summary>
    /// Set a target for an action.
    /// </summary>
    /// <param name="actionId">The correlation id of the action.</param>
    /// <param name="targetNode">The target node.</param>
    /// <exception cref="ActionNotFoundException">Thrown if no action with was found using the action id</exception>
    /// <param name="route">The route, if any.</param>
    public void SetTargetForAction(Guid actionId, GraphNode targetNode, Route? route = null)
    {
        if (Actions.All(a => a.CorrelationId != actionId))
        {
            throw new ActionNotFoundException("Action not found using id", "CorrelationId", actionId);
        }
        var action = Actions.Single(a => a.CorrelationId == actionId);
        action.SetTarget(targetNode);
        if (route != null)
        {
            action.Nodes = route.Nodes;
            action.Edges = route.Edges;
        }
        var failReason = Reasons.FirstOrDefault(r => string.Equals(r.Template,
                                                         Constants.Reasons.ActionFailedWrongTargetType) ||
                                                     string.Equals(r.Template,
                                                         Constants.Reasons.ActionFailedLocked));
        if (action.State == ActionState.Failed && failReason != null)
        {
            action.State = ActionState.Started;
            State = ActionGroupState.Started;
            Reasons.Remove(failReason);
        }
    }

    private void UpdateActionState(Guid actionId, ActionState state, DateTimeOffset now)
    {
        Actions.Single(a => a.CorrelationId == actionId).UpdateActionState(state, now);
        UpdateActionGroupState();
    }

    /// <summary>
    /// Cancels the <see cref="ActionGroup"/> and all the child <see cref="AgvAction"/>.
    /// </summary>
    /// <returns></returns>
    /// <param name="now">The current time.</param>
    public bool CancelActionGroup(DateTimeOffset now)
    {
        if (Actions.Any(a => a.State != ActionState.Created))
        {
            return false;
        }
        foreach (var agvAction in Actions)
        {
            CancelAction(agvAction.CorrelationId, now);
        }
        return true;
    }

    /// <summary>
    /// Resets the <see cref="ActionGroup"/> and all <see cref="AgvAction"/> to
    /// <see cref="ActionState.Created"/>.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <param name="resetAssignedAgv">If true, the assigned agv is set to null.</param>
    public void ResetActionGroup(DateTimeOffset now, bool resetAssignedAgv = true)
    {
        foreach (var agvAction in Actions)
        {
            ResetAction(agvAction, now);
        }
        if (resetAssignedAgv)
        {
            AssignedAgv = null;
        }
        StartedDate = null;
        CompletedDate = null;
    }

    /// <summary>
    /// Cancels the <see cref="ActionGroup"/> and all the child <see cref="AgvAction"/>.
    /// </summary>
    /// <param name="reasons"></param>
    /// <returns></returns>
    /// <param name="now">The current time.</param>
    public void InvalidateActionGroup(IEnumerable<Reason> reasons, DateTimeOffset now)
    {
        Reasons.AddRange(reasons);
        foreach (var agvAction in Actions)
        {
            InvalidateAction(agvAction.CorrelationId, now);
        }
    }

    /// <summary>
    /// Get the targets of the action group.
    /// </summary>
    /// <returns>Node ids if the targets.</returns>
    public Guid[] GetTargets()
    {
        return !Actions.Any() ?
            [] :
            Actions
                .Where((action, index) => !action.IsIncomplete &&
                                          (index == 0 || (!Actions[index - 1].IsIncomplete && action.Sequence == Actions[index - 1].Sequence + 1)))
                .Select(action => action.Target.Node!.Id)
                .ToArray();
    }

    /// <summary>
    /// Set dates from given duration values (for unit testing).
    /// </summary>
    /// <param name="actionDuration">The duration of each action [s].</param>
    /// <param name="startDate">The start date.</param>
    /// <param name="idleTimeBetweenActions">The idle time between actions [s].</param>
    public void SetFixedDatesFromDurations(double? actionDuration, DateTimeOffset startDate, double? idleTimeBetweenActions)
    {
        StartedDate = startDate.UtcDateTime;
        foreach (var action in Actions)
        {
            action.StartedDate = startDate.UtcDateTime;
            if (actionDuration != null)
            {
                action.CompletedDate = startDate.AddSeconds((double)actionDuration).UtcDateTime;
                startDate = action.CompletedDate.Value.AddSeconds(idleTimeBetweenActions.GetValueOrDefault(0));
            }
        }
        CompletedDate = Actions[^1].CompletedDate;
    }

    /// <summary>
    /// Skips the provided action and updates the previous one with its type and the new target.
    /// </summary>
    /// <param name="actionId">The action id to skip.</param>
    /// <param name="targetNode">The id of the new target node.</param>
    /// <param name="route">The route, if any.</param>
    public AgvAction SetTargetForActionWithUpdate(Guid actionId, GraphNode targetNode, Route? route = null)
    {
        if (Actions.All(a => a.CorrelationId != actionId))
        {
            throw new ActionNotFoundException("Action not found using id", "CorrelationId", actionId);
        }

        var toSkip = Actions.First(a => a.CorrelationId == actionId);
        var toUpdate = Actions.OrderBy(a => a.Sequence).Last(a => a.Sequence < toSkip.Sequence);
        toUpdate.ChangeActiontype(toSkip.ActionType);
        toUpdate.SetTarget(targetNode);
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
    public bool IsOutdated(TimeSpan expirationTime, DateTimeOffset now)
    {
        var expirationDate = now.Add(-expirationTime);
        if (StartedDate < expirationDate)
        {
            return true;
        }
        return CreatedDate < expirationDate;
    }

    /// <summary>
    /// Check if the given load state matches the state of the last completed
    /// or currently running action.
    /// </summary>
    /// <param name="agvHasCargo">Load state of the agv.</param>
    /// <param name="assignedAgv">The correlation id of the agv.</param>
    /// <returns>True, if the load state matches the action group state.</returns>
    public bool CheckLoadStateMatches(bool agvHasCargo, Guid assignedAgv)
    {
        if (AssignedAgv != assignedAgv)
        {
            throw new AgvNotAssignedToActionGroupException(assignedAgv, CorrelationId);
        }

        if (!IsMovingCargo)
        {
            return !agvHasCargo;
        }

        List<AgvAction> picks = Actions.Where(a => a.ActionType == ActionType.Pick).ToList();
        List<AgvAction> drops = Actions.Where(a => a.ActionType == ActionType.Drop).ToList();

        ActionState? lastPickState = picks.MaxBy(a => a.StartedDate)?.State;
        ActionState? lastDropState = drops.MaxBy(a => a.StartedDate)?.State;

        bool pickCompleted = lastPickState == ActionState.Completed;
        bool dropCompleted = lastDropState == ActionState.Completed;

        bool hasPick = picks.Any();
        bool hasDrop = drops.Any();

        // Single action type (only Pick or only Drop)
        if (!hasDrop)
        {
            return agvHasCargo == pickCompleted;
        }

        if (!hasPick)
        {
            return agvHasCargo != dropCompleted;
        }

        // Mixed Pick & Drop actions
        return agvHasCargo ? pickCompleted && !dropCompleted : dropCompleted || !pickCompleted;
    }
}