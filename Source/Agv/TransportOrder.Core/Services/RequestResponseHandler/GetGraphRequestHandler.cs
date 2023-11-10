using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.MessageContent;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv graph requests.
/// </summary>
[UsedImplicitly]
public class GetGraphRequestHandler :
    IConsumer<GetAgvGraph>,
    IConsumer<GetAgvGraphs>,
    IConsumer<GetMapGraph>
{
    private readonly IAgvRepository _agvRepository;
    private readonly IMapper _mapper;
    private readonly IAgvGraphRepository _agvGraphRepository;

    /// <summary>
    /// Initializes a new  <see cref="GetGraphRequestHandler"/> instance.
    /// </summary>
    public GetGraphRequestHandler(IAgvRepository agvRepository, IMapper mapper, IAgvGraphRepository agvGraphRepository)
    {
        _agvRepository = agvRepository;
        _mapper = mapper;
        _agvGraphRepository = agvGraphRepository;
    }

    /// <summary>
    /// Handles the <see cref="GetAgvGraph"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    public async Task Consume(ConsumeContext<GetAgvGraph> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvRepository.GetAgvAsync(correlationId);

        var agvGraph = await _agvGraphRepository.GetAgvGraphAsync(agv.CorrelationId);

        var graph = _mapper.Map<Graph>(agvGraph);
        await context.RespondAsync(new GotAgvGraph(graph));
    }

    /// <summary>
    /// Handles the <see cref="GetAgvGraphs"/> request.
    /// </summary>
    /// <exception cref="AgvsNotFoundException"></exception>
    /// <exception cref="AgvGraphsNotDefinedException"></exception>
    public async Task Consume(ConsumeContext<GetAgvGraphs> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;

        ICollection<Domain.Models.Agv.Agv> agvs = await _agvRepository.SelectExistingAgvsAsync(selectedAgvs.ToList());
        if (agvs.Count != selectedAgvs.Count)
        {
            throw new AgvsNotFoundException(selectedAgvs.Except(agvs.Select(a => a.CorrelationId)).ToList());
        }

        ICollection<AgvGraph> agvGraphs = await _agvGraphRepository.GetSelectedAgvGraphsAsync(selectedAgvs);
        List<Guid> undefinedGraphs = selectedAgvs.Except(agvGraphs.Select(g => g.CorrelationId)).ToList();
        if (undefinedGraphs.Any())
        {
            throw new AgvGraphsNotDefinedException(undefinedGraphs);
        }
        var graphs = _mapper.Map<List<Graph>>(agvGraphs);
        await context.RespondAsync(new GotAgvGraphs(graphs));
    }

    /// <summary>
    /// Handles the <see cref="GetMapGraph"/>
    /// </summary>
    /// <exception cref="MapNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetMapGraph> context)
    {
        var correlationId = context.Message.CorrelationId;
        var graph = await _agvGraphRepository.GetGraphForMap(correlationId);
        await context.RespondAsync(new GotMapGraph(_mapper.Map<Graph>(graph)));
    }
}