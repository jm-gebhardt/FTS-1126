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
        /// Action to be executed
        /// </summary>
        public AgvAction Action { get; }

        /// <summary>
        /// Costructor
        /// </summary>
        /// <param name="correlationId">CorrelationId</param>
        /// <param name="action">Action to be executed</param>
        public StartActionCommand(Guid correlationId, AgvAction action)
        {
            CorrelationId = correlationId;
            Action = action;
        }
    }
}