using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;

/// <summary>
/// Gets the data from the <see cref="GetAgvDtoRequestHandler"/> and throws if there was no data
/// </summary>
[UsedImplicitly]
public class GetAgvDtoRequestHandler : IConsumer<GetAgvDtoRequest>
{
    private readonly IRequestClient<GetAgvsDtoRequest> _requestClient;

    /// <summary>
    /// <inheritdoc cref="GetAgvsDtoRequestHandler"/>
    /// </summary>
    public GetAgvDtoRequestHandler(IRequestClient<GetAgvsDtoRequest> requestClient)
    {
        _requestClient = requestClient;
    }

    /// <inheritdoc cref="GetAgvDtoRequestHandler"/>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Consume(ConsumeContext<GetAgvDtoRequest> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        Response<GetAgvsDtoResult> result = await _requestClient.GetResponse<GetAgvsDtoResult>(new GetAgvsDtoRequest(new List<Guid> {agvCorrelationId}));
        if (!result.Message.Agvs.Any())
        {
            throw new AgvNotFoundException(agvCorrelationId);
        }
        await context.RespondAsync(new GetAgvDtoResult(result.Message.Agvs.Single()));
    }
}