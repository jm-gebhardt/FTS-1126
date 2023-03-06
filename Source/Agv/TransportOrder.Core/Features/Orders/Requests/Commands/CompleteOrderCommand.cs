using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Commands
{
    /// <summary>
    /// Represents the CompleteOrder request which completes order manually
    /// </summary>
    public record CompleteOrderCommand : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// <inheritdoc cref="CompleteOrderCommand"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public CompleteOrderCommand(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
