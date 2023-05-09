using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Maps;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Maps.Handlers.Queries
{
    /// <summary>
    /// Defines a handler for the GetMapImage request
    /// </summary>
    [UsedImplicitly]
    public class GetMapImageRequestHandler : IConsumer<GetMapImageRequest>
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
        /// <param name="context"></param>
        /// <returns>A map from the MapRegistry</returns>
        /// <exception cref="MapNotFoundException">Map is null or empty</exception>
        public async Task Consume(ConsumeContext<GetMapImageRequest> context)
        {
            var request = context.Message;

            byte[]? image = _mapRegistry.GetMap(request.CorrelationId);
            if (image == null || image.Length == 0)
            {
                throw new MapNotFoundException(nameof(request.CorrelationId), request.CorrelationId);
            }

            await context.RespondAsync(new GetMapImageResult(image));
        }
    }
}
