using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries
{
    /// <summary>
    /// Represents the GetAvgStatus request which returns AgvState
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public record GetAgvStatusRequest : CorrelatedBy<Guid>
    {
        /// <inheritdoc/>
        public Guid CorrelationId { get; }

        /// <summary>
        /// <inheritdoc cref="GetAgvStatusRequest"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public GetAgvStatusRequest(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}