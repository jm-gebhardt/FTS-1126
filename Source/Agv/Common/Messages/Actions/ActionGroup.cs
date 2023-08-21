using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions;

/// <summary>
/// Grouping actions and sending them across service modules
/// </summary>
public class ActionGroup : CorrelatedBy<Guid>
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
    /// The creator of the <see cref="ActionGroup"/>.
    /// </summary>
    public ActionGroupCreator Creator { get; set; }

    /// <summary>
    ///  Initializes a new <see cref="ActionGroup"/> instance.
    /// </summary>
    /// <param name="correlationId">The CorrelationId of the <see cref="ActionGroup"/></param>
    /// <param name="actions">The child Actions of the <see cref="ActionGroup"/></param>
    /// <param name="orderId">The OrderID</param>
    /// <param name="assignedAgv">The assigned Agv of this <see cref="ActionGroup"/></param>
    /// <param name="creator">The creator of this <see cref="ActionGroup"/></param>
    /// <param name="createdDate">The date of the creation of this <see cref="ActionGroup"/></param>
    /// <param name="isManual">If true, then this <see cref="ActionGroup"/> was created manually, not by the system</param>
    public ActionGroup(Guid correlationId, string orderId, ICollection<AgvAction> actions, ActionGroupCreator creator, DateTime? createdDate = default, Guid? assignedAgv = null, bool isManual = false) : this(correlationId, orderId)
    {
        AssignedAgv = assignedAgv;
        IsManual = isManual;
        CreatedDate = createdDate.GetValueOrDefault(DateTime.UtcNow);
        Actions = actions;
        Creator = creator;
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