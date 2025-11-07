using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Common.Messages.ActionGroups;

/// <summary>
/// Grouping actions and sending them across service modules
/// </summary>
[PublicAPI]
public record ActionGroup : CorrelatedBy<Guid>
{
    /// <inheritdoc />
    /// Parenting ID
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// A unique ID of the order that clamps all the corresponding actions
    /// </summary>
    public string OrderId { get; init; }

    /// <summary>
    /// Assigned AGV that will execute the ActionGroup
    /// </summary>
    public Guid? AssignedAgv { get; set; }

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
    /// The estimated duration of the whole ActionGroup based on the estimated duration of the actions.
    /// </summary>
    public TimeSpan? EstimatedDuration { get; set; }

    /// <summary>
    /// State of the ActionGroup
    /// </summary>
    public ActionGroupState State { get; set; } = ActionGroupState.Created;

    /// <summary>
    /// List of actions
    /// </summary>
    public ICollection<AgvAction> Actions { get; init; }

    /// <summary>
    /// If true, then this ActionGroup was created manually, not by the system
    /// </summary>
    public bool IsManual { get; set; }

    /// <summary>
    /// The priority of the action group.
    /// </summary>
    public Priority Priority { get; init; }

    /// <summary>
    /// If true, then this ActionGroup allows to trigger escaping from the queue
    /// </summary>
    public bool ShouldEscapeQueue { get; init; }

    /// <summary>
    /// Whether this action group is only relevant in the current system context.
    /// Action groups with this flag will be canceled/aborted in certain mode switches
    /// and should be of a type to be recreated automatically when the mode is switched back if they are still relevant.
    /// E.g. Queue entry action groups will be recreated when a queue is still under capacity.
    /// </summary>
    public bool DoNotPause { get; }

    /// <summary>
    /// The creator of the <see cref="ActionGroup"/>.
    /// </summary>
    public ActionGroupCreator Creator { get; set; }

    /// <summary>
    /// Are set when the ActionGroup went into a not successful state, like invalid, aborted or canceled.
    /// </summary>
    public List<Reason> Reasons { get; set; } = new();

    /// <summary>
    /// The handling unit corresponding to this ActionGroup.
    /// </summary>
    public string? HandlingUnit { get; set; }

    /// <summary>
    /// True, if the action group has been queued.
    /// </summary>
    public bool IsQueued { get; set; }

    /// <inheritdoc cref="ActionGroup"/>
    public ActionGroup(Guid correlationId, string orderId, ICollection<AgvAction> actions, ActionGroupCreator creator, Priority priority = Priority.Default, bool shouldEscapeQueue = false, DateTime? createdDate = null, Guid? assignedAgv = null, bool isManual = false, bool isQueued = false, bool doNotPause = false) : this(correlationId, orderId)
    {
        AssignedAgv = assignedAgv;
        IsManual = isManual;
        CreatedDate = createdDate.GetValueOrDefault(DateTime.UtcNow);
        Actions = actions;
        Creator = creator;
        IsQueued = isQueued;
        DoNotPause = doNotPause;
        Priority = priority;
        ShouldEscapeQueue = shouldEscapeQueue;
    }

    /// <summary>
    /// Initializes a new <see cref="ActionGroup"/> instance.
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the ActionGroup</param>
    /// <param name="orderId">The OrderID</param>
    public ActionGroup(Guid correlationId, string orderId)
    {
        CorrelationId = correlationId;
        OrderId = orderId;
        Actions = new List<AgvAction>();
    }

    /// <summary>
    /// Json ctor
    /// </summary>
    public ActionGroup()
    {
        OrderId = "";
        Actions = new List<AgvAction>();
    }
}