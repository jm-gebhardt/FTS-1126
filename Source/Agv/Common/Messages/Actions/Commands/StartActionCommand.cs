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
        /// A unique ID of the order that clamps all the corresponding actions
        /// </summary>
        public string OrderId { get; }

        /// <summary>
        /// Action to be executed
        /// </summary>
        public AgvAction Action { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="agvId">Id of the agv the action is send to</param>
        /// <param name="orderId">A unique ID of the order that clamps all the corresponding actions</param>
        /// <param name="action">Action to be executed</param>
        public StartActionCommand(Guid correlationId, string agvId, string orderId, AgvAction action)
        {
            CorrelationId = correlationId;
            Action = action;
            AgvId = agvId;
            OrderId = orderId;
        }
    }
}