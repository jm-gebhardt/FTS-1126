using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Commands
{
    /// <summary>
    /// Represents the EmitRequestData request
    /// </summary>
    public record RequestAgvDataCommand : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// <inheritdoc cref="RequestAgvDataCommand"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public RequestAgvDataCommand(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
