using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Handlers.Queries;

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

        var agvDataDto = _mapper.Map<AgvDataDto>(agvData);
        await context.RespondAsync(new GetAgvDataResult(agvDataDto));
    }
}