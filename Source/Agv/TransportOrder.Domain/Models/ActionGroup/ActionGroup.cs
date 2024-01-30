using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Models;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;

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
    /// The first action to be executed.
    /// </summary>
    public AgvAction? FirstNotStartedAction => Actions.OrderBy(a => a.Sequence).FirstOrDefault(a => a.State == ActionState.Created);

    private readonly List<AgvAction> _actions;

    /// <summary>
    /// List of actions.
    /// </summary>
    public IReadOnlyCollection<AgvAction> Actions => _actions.AsReadOnly();

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
    public ActionGroup(Guid correlationId, string orderId, IEnumerable<AgvAction> actions, ActionGroupCreator creator, DateTime? createdDate = default, Guid? assignedAgv = null, bool isManual = false) : this(correlationId, orderId)
    {
        AssignedAgv = assignedAgv;
        IsManual = isManual;
        CreatedDate = createdDate.GetValueOrDefault(DateTime.UtcNow);
        _actions = actions.OrderBy(a => a.Sequence).ToList();
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
        _actions = new List<AgvAction>();
    }

    /// <summary>
    /// Json ctor
    /// </summary>
    public ActionGroup()
    {
        OrderId = "";
        _actions = new List<AgvAction>();
    }

    /// <summary>
    /// Updates the state of the ActionGroup based on the state of its actions.
    /// </summary>
    public void UpdateActionGroupState()
    {
        var previousState = State;

        if (!_actions.Any())
        {
            throw new ActionsInActionGroupEmptyException(CorrelationId);
        }

        State = _actions switch
        {
            _ when _actions.All(a => a.State == ActionState.Completed) => ActionGroupState.Completed,
            _ when _actions.All(a => a.State == ActionState.Created) => ActionGroupState.Created,
            _ when _actions.Any(a => a.State == ActionState.Invalid) => ActionGroupState.Invalid,
            _ when _actions.Any(a => a.State == ActionState.Failed) => ActionGroupState.Failed,
            _ when _actions.Any(a => a.State == ActionState.Canceled) => ActionGroupState.Canceled,
            _ when _actions.Any(a => a.State == ActionState.Aborted) => ActionGroupState.Aborted,
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
    public void StartAction(Guid actionId)
    {
        UpdateActionState(actionId, ActionState.Started);
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
}