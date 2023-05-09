using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands
{
    /// <summary>
    /// Represents the SetIdleRequest request
    /// </summary>
    public record UnlockAgvCommand : CorrelatedBy<Guid>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// <inheritdoc cref="UnlockAgvCommand"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public UnlockAgvCommand(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
