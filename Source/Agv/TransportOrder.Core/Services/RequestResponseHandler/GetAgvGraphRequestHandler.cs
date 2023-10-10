using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for GetAgvGraph request
/// Gets agv from registry by correlationId and return AgvGraph
/// </summary>
[UsedImplicitly]
public class GetAgvGraphRequestHandler : IConsumer<GetAgvGraph>
{
    private readonly IAgvRepository _agvRepository;
    private readonly IMapper _mapper;
    private readonly IAgvGraphRegistry _agvGraphRegistry;

    /// <summary>
    /// Initializes a new  <see cref="GetAgvGraphRequestHandler"/> instance.
    /// </summary>
    public GetAgvGraphRequestHandler(IAgvRepository agvRepository, IMapper mapper, IAgvGraphRegistry agvGraphRegistry)
    {
        _agvRepository = agvRepository;
        _mapper = mapper;
        _agvGraphRegistry = agvGraphRegistry;
    }

    /// <summary>
    /// <inheritdoc cref="GetAgvGraphRequestHandler"/>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="AgvNotFoundException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    public async Task Consume(ConsumeContext<GetAgvGraph> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvRepository.GetAgvAsync(correlationId);
        if (agv == null)
        {
            throw new AgvNotFoundException(correlationId);
        }

        var agvGraph = await _agvGraphRegistry.GetAgvGraphAsync(agv.CorrelationId);
            
        var graph = _mapper.Map<Graph>(agvGraph);
        await context.RespondAsync(new GotAgvGraph(graph));
    }
}