using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for GetAgvData request
/// Gets agv from registry by correlationId returns AgvData
/// </summary>
[UsedImplicitly]
public class GetAgvDataRequestHandler : IConsumer<GetAgvDataRequest>
{
    private readonly IAgvDataRegistry _agvDataRegistry;
    private readonly IMapper _mapper;

    /// <summary>
    /// <inheritdoc cref="GetAgvDataRequestHandler"/>
    /// </summary>
    /// <param name="agvDataRegistry"></param>
    /// <param name="mapper"></param>
    public GetAgvDataRequestHandler(IAgvDataRegistry agvDataRegistry, IMapper mapper)
    {
        _agvDataRegistry = agvDataRegistry;
        _mapper = mapper;
    }

    /// <summary>
    /// <inheritdoc cref="GetAgvDataRequestHandler"/>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    /// <exception cref="AgvDataNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetAgvDataRequest> context)
    {
        var request = context.Message;
        var agvData = _agvDataRegistry.GetAgvData(request.CorrelationId);
        if (agvData == null)
        {
            throw new AgvDataNotFoundException(request.CorrelationId);
        }

        var agvDataMessageContent = _mapper.Map<AgvData>(agvData);
        await context.RespondAsync(new GetAgvDataResult(agvDataMessageContent));
    }
}