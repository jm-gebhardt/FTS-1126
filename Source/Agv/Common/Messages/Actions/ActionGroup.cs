using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions
{
    /// <summary>
    /// Grouping actions and sending them across service modules
    /// </summary>
    public record ActionGroup : CorrelatedBy<Guid>
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
        public Guid? AssignedAgv;

        /// <summary>
        /// Date the action was created
        /// </summary>
        public DateTime DtCreated;

        /// <summary>
        /// Date the action has started
        /// </summary>
        public DateTime? DtStarted;

        /// <summary>
        /// Date the action has completed
        /// </summary>
        public DateTime? DtCompleted;

        /// <summary>
        /// State of the action group
        /// </summary>
        public ActionGroupState State;
        
        /// <summary>
        /// List of actions
        /// </summary>
        public AgvAction[] Actions { get; }

        /// <summary>
        /// Generating an instance of <see cref="ActionGroup"/>
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="dtCompleted"></param>
        /// <param name="state"></param>
        /// <param name="actions"></param>
        /// <param name="orderId"></param>
        /// <param name="assignedAgv"></param>
        /// <param name="dtCreated"></param>
        /// <param name="dtStarted"></param>
        public ActionGroup(Guid correlationId, string orderId, AgvAction[] actions, DateTime dtCreated, DateTime? dtStarted = null, DateTime? dtCompleted = null, ActionGroupState state = ActionGroupState.Created, Guid? assignedAgv = null)
        {
            CorrelationId = correlationId;
            OrderId = orderId;
            AssignedAgv = assignedAgv;
            DtCreated = dtCreated;
            DtStarted = dtStarted;
            DtStarted = dtCompleted;
            State = state;
            DtCompleted = dtCompleted;
            Actions = actions;
        }

        /// <summary>
        /// Constructor which auto creates Started date using DateTime.Now
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="orderId"></param>
        /// <param name="actions"></param>
        public ActionGroup(Guid correlationId, string orderId, AgvAction[] actions)
        {
            CorrelationId = correlationId;
            Actions = actions;
            OrderId = orderId;
            DtCreated = DateTime.Now;
        }
    }
}
