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
        /// Assigned AGV that will execute the action group
        /// </summary>
        public Guid? AssignAgv;

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
        public ActionState State;

        /// <summary>
        /// List of actions
        /// </summary>
        public List<AgvAction> Actions { get; }

        /// <summary>
        /// Generating an instance of <see cref="ActionGroup"/>
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="dtCompleted"></param>
        /// <param name="actions"></param>
        /// <param name="assignAgv"></param>
        /// <param name="dtCreated"></param>
        /// <param name="dtStarted"></param>
        public ActionGroup(Guid correlationId, Guid? assignAgv, DateTime dtCreated, DateTime? dtStarted, DateTime? dtCompleted, List<AgvAction> actions)
        {
            CorrelationId = correlationId;
            AssignAgv = assignAgv;
            DtCreated = dtCreated;
            DtStarted = dtStarted;
            DtStarted = dtCompleted;
            DtCompleted = dtCompleted;
            Actions = actions;
        }

        /// <summary>
        /// Constructor which auto creates Started date using DateTime.Now
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="actions"></param>
        public ActionGroup(Guid correlationId, List<AgvAction> actions)
        {
            CorrelationId = correlationId;
            Actions = actions;
            DtCreated = DateTime.Now;
        }
    }
}
