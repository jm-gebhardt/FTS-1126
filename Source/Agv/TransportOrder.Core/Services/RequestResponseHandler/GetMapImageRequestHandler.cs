using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for the GetMapImage request
/// </summary>
[UsedImplicitly]
public class GetMapImageRequestHandler : IConsumer<GetMapImage>
{
    private readonly IMapRepository _mapRepository;

    /// <summary>
    /// <inheritdoc cref="GetMapImageRequestHandler"/>
    /// </summary>
    /// <param name="mapRepository"></param>
    public GetMapImageRequestHandler(IMapRepository mapRepository)
    {
        _mapRepository = mapRepository;
    }

    /// <summary>
    /// Handles a GetMapImage request.
    /// </summary>
    /// <param name="context"></param>
    /// <returns>A map from the MapRepository.</returns>
    /// <exception cref="MapNotFoundException">Map is null or empty</exception>
    public async Task Consume(ConsumeContext<GetMapImage> context)
    {
        var request = context.Message;

        byte[]? image = _mapRepository.GetMapImage(request.CorrelationId);
        if (image == null || image.Length == 0)
        {
            throw new MapNotFoundException(request.CorrelationId);
        }

        await context.RespondAsync(new GotMapImage(image));
    }
}