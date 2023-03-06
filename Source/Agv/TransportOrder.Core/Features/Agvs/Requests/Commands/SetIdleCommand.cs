using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands
{
    /// <summary>
    /// Represents the SetIdleRequest request
    /// </summary>
    public record SetIdleCommand : CorrelatedBy<Guid>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// <inheritdoc cref="SetIdleCommand"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public SetIdleCommand(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
