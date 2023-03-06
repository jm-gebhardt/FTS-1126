using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries
{
    /// <summary>
    /// Represents the GetAgvData request which returns AgvData
    /// </summary>
    public record GetAgvDataRequest : CorrelatedBy<Guid>
    {

        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// <inheritdoc cref="GetAgvDataRequest"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public GetAgvDataRequest(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
