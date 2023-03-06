using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Commands
{
    /// <summary>
    /// Represents the ResetOrder request which resets the order and starts a new transport
    /// </summary>
    public record ResetOrderCommand : CorrelatedBy<Guid>
    {

        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// <inheritdoc cref="ResetOrderCommand"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public ResetOrderCommand(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
