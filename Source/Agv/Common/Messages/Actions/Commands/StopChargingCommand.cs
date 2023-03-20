using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands
{
    /// <summary>
    /// Triggers the stop charging of an Agv on a charging node
    /// </summary>
    /// <value></value>
    public record StopChargingCommand : CorrelatedBy<Guid>
    {
        /// <summary>
        /// The CorrelationId of the Agv
        /// </summary>
        public Guid CorrelationId {get;}

        /// <summary>
        /// <inheritdoc cref="StopChargingCommand"/>
        /// </summary>
        /// <param name="correlationId">The CorrelationId of the Agv</param>

        public StopChargingCommand(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}