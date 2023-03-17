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
        /// List of actions
        /// </summary>
        public List<AgvAction> ActionList { get; }

        /// <summary>
        /// Generating an instance of <see cref="ActionGroup"/>
        /// </summary>
        /// <param name="correlationId"></param>
        /// <param name="actionList"></param>
        public ActionGroup(Guid correlationId, List<AgvAction> actionList)
        {
            CorrelationId = correlationId;
            ActionList = actionList;
        }
    }
}
