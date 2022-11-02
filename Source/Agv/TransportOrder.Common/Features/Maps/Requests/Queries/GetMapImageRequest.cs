using MassTransit;
using MediatR;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Common.Features.Maps.Requests.Queries
{
    /// <summary>
    /// Represents the GetMap request which returns an image of the map as byte[]
    /// </summary>
    public class GetMapImageRequest : CorrelatedBy<Guid>, IRequest<byte[]>
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Guid CorrelationId { get; }

        /// <summary>
        /// <inheritdoc cref="GetMapImageRequest"/>
        /// </summary>
        /// <param name="correlationId"></param>
        public GetMapImageRequest(Guid correlationId)
        {
            CorrelationId = correlationId;
        }
    }
}
