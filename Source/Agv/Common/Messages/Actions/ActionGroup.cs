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
        public Guid CorrelationId { get; }

        /// <summary>
        /// A unique ID of the order that clamps all the corresponding actions
        /// </summary>
        public string OrderId { get; init; }

        /// <summary>
        /// Assigned AGV that will execute the action group
        /// </summary>
        public Guid? AssignedAgv { get; set; }

        /// <summary>
        /// Date the action was created
        /// </summary>
        public DateTime CreatedDate { get; init; }

        /// <summary>
        /// Date the action has started
        /// </summary>
        public DateTime? StartedDate { get; init; }

        /// <summary>
        /// Date the action has completed
        /// </summary>
        public DateTime? CompletedDate { get; init; }

        /// <summary>
        /// State of the action group
        /// </summary>
        public ActionGroupState State { get; set; } = ActionGroupState.Created;

        /// <summary>
        /// List of actions
        /// </summary>
        public List<AgvAction> Actions { get; init; } = new();

        /// <summary>
        /// Generating an instance of <see cref="ActionGroup"/>
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="completedDate"></param>
        /// <param name="state"></param>
        /// <param name="actions"></param>
        /// <param name="orderId"></param>
        /// <param name="assignedAgv"></param>
        /// <param name="createdDate"></param>
        /// <param name="startedDate"></param>
        public ActionGroup(Guid correlationId, string orderId, List<AgvAction> actions, DateTime createdDate, DateTime? startedDate = null, DateTime? completedDate = null, ActionGroupState state = ActionGroupState.Created, Guid? assignedAgv = null)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
            AssignedAgv = assignedAgv;
            CreatedDate = createdDate;
            StartedDate = startedDate;
            StartedDate = completedDate;
            State = state;
            CompletedDate = completedDate;
            Actions = actions;
        }

        /// <summary>
        /// Constructor which auto creates Started date using DateTime.Now
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="orderId"></param>
        /// <param name="actions"></param>
        public ActionGroup(Guid correlationId, string orderId, List<AgvAction> actions)
        {
            CorrelationId = correlationId;
            Actions = actions;
            OrderId = orderId;
            CreatedDate = DateTime.Now;
        }

        /// <summary>
        /// Json ctor
        /// </summary>
        public ActionGroup()
        {
            OrderId = "";
        }
    }
}