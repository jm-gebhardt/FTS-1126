using MassTransit;

namespace Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands
{
    /// <summary>
    /// Triggers the charging of an Agv on a charging node
    /// </summary>
    /// <value></value>
    public record StartChargingCommand : CorrelatedBy<Guid>
    {
        /// <summary>
        /// The CorrelationId of the Agv
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// <inheritdoc cref="StartChargingCommand"/>
        /// </summary>
        /// <param name="correlationId">The CorrelationId of the Agv</param>
        public StartChargingCommand(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}