using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Contracts;
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
    /// Target node id of the action
    /// </summary>
    public string TargetNodeId { get; init; }

    /// <summary>
    /// The nodes of the trip
    /// </summary>
    public string[] Nodes { get; set; } = Array.Empty<string>();

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
    /// The duration of the action.
    /// </summary>
    public TimeSpan? Duration => CompletedDate - StartedDate;

    /// <summary>
    /// Action is the first action of a group.
    /// </summary>
    public bool IsFirstAction => Sequence == 0;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="correlationId">Unique correlation ID of this action (see order action repository)</param>
    /// <param name="actionType">The type of the action, e.g. Pick or Drop</param>
    /// <param name="targetNodeId">Target node id of the action</param>
    /// <param name="parentId">The CorrelationId of the ActionGroup where this action belongs to</param>
    public AgvAction(Guid correlationId, ActionType actionType, string targetNodeId, Guid parentId)
    {
        CorrelationId = correlationId;
        ActionType = actionType;
        TargetNodeId = targetNodeId;
        ParentId = parentId;
    }

    /// <summary>
    /// Just to satisfy Entity Framework
    /// </summary>
    [UsedImplicitly]
    private AgvAction()
    {
        CorrelationId = Guid.Empty;
        ActionType = ActionType.Pick;
        TargetNodeId = string.Empty;
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

        State = state;

        SetDates();
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
        }
    }

    /// <summary>
    /// Simple string representation for logging.
    /// </summary>
    public override string ToString()
    {
        return $"Nodes: {string.Join(",", Nodes)} TargetNode: {TargetNodeId} CorrelationId: {CorrelationId}, ParentId: {ParentId}";
    }
}