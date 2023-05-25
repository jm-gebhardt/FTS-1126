using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions
{
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
        /// Generating an instance of <see cref="ActionGroup"/>
        /// </summary>
        /// <param name="correlationId">The CorrelationId of the ActionGroup</param>
        /// <param name="actions">The child Actions of the ActionGroup</param>
        /// <param name="orderId">The OrderID</param>
        /// <param name="assignedAgv">The assigned Agv of this ActionGroup</param>
        /// <param name="createdDate">The date of the creation of this ActionGroup</param>
        public ActionGroup(Guid correlationId, string orderId, ICollection<AgvAction> actions, DateTime? createdDate = default, Guid? assignedAgv = null) : this(correlationId, orderId)
        {
            AssignedAgv = assignedAgv;
            CreatedDate = createdDate.GetValueOrDefault(DateTime.UtcNow);
            Actions = actions;
        }

        /// <summary>
        /// Constructor which auto creates Started date using DateTime.Now
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
}