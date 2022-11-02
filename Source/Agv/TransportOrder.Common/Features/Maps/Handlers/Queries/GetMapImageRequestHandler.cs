using Gebhardt.StoreWare.Agv.TransportOrder.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using MediatR;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Common.Features.Maps.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetMapImage request
    /// </summary>
    public class GetMapImageRequestHandler : IRequestHandler<GetMapImageRequest, byte[]>
    {
        private readonly IMapRegistry _mapRegistry;

        /// <summary>
        /// <inheritdoc cref="GetMapImageRequestHandler"/>
        /// </summary>
        /// <param name="mapRegistry"></param>
        public GetMapImageRequestHandler(IMapRegistry mapRegistry)
        {
            _mapRegistry = mapRegistry;
        }

        /// <summary>
        /// Handles a GetMapImage request
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>A map from the MapRegistry</returns>
        /// <exception cref="MapNotFoundException">Map is null or empty</exception>
        public Task<byte[]> Handle(GetMapImageRequest request, CancellationToken cancellationToken)
        {
            byte[]? image = _mapRegistry.GetMap(request.CorrelationId);
            if (image == null || image.Length == 0)
            {
                throw new MapNotFoundException($"Map not found using the correlationId:{request.CorrelationId}");
            }

            return Task.FromResult(image);
        }
    }
}
