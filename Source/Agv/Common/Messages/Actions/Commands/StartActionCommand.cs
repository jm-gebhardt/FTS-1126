using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands
{
    /// <summary>
    /// Triggers the action on the Agv side
    /// </summary>
    public record StartActionCommand : CorrelatedBy<Guid>
    {
        /// <inheritdoc />
        public Guid CorrelationId { get; }
        
        /// <summary>
        /// Id of the agv the action is send to
        /// </summary>
        public string AgvId { get; }

        /// <summary>
        /// Action to be executed
        /// </summary>
        public AgvAction Action { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="agvId">Id of the agv the action is send to</param>
        /// <param name="action">Action to be executed</param>
        public StartActionCommand(Guid correlationId, string agvId, AgvAction action)
        {
            CorrelationId = correlationId;
            Action = action;
            AgvId = agvId;
        }
    }
}