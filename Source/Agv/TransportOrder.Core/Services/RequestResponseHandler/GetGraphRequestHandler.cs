using AutoMapper;
using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agv.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.Acc.TransportOrder.Messages.Requests.Map;
using JetBrains.Annotations;
using MassTransit;
using Graph = Gebhardt.Acc.TransportOrder.Messages.MessageContent.Graph;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for agv graph requests.
/// </summary>
[UsedImplicitly]
public class GetGraphRequestHandler :
    IConsumer<GetAgvGraph>,
    IConsumer<GetAgvGraphs>,
    IConsumer<GetMapGraph>,
    IConsumer<GetGraph>
{
    private readonly IMapper _mapper;
    private readonly IAgvDb _agvDb;

    /// <summary>
    /// Initializes a new  <see cref="GetGraphRequestHandler"/> instance.
    /// </summary>
    public GetGraphRequestHandler(IAgvDb agvDb, IMapper mapper)
    {
        _agvDb = agvDb;
        _mapper = mapper;
    }

    /// <summary>
    /// Handles the <see cref="GetAgvGraph"/> request.
    /// </summary>
    /// <exception cref="AgvNotFoundException"></exception>
    /// <exception cref="AgvGraphNotDefinedException"></exception>
    public async Task Consume(ConsumeContext<GetAgvGraph> context)
    {
        var correlationId = context.Message.CorrelationId;
        var agv = await _agvDb.Agvs.GetAgvAsync(correlationId, context.CancellationToken);

        var agvGraph = await _agvDb.Graphs.GetAgvGraphAsync(agv.CorrelationId, context.CancellationToken);

        var graph = _mapper.Map<Graph>(agvGraph);
        await context.RespondAsync(new GotAgvGraph(graph));
    }

    /// <summary>
    /// Handles the <see cref="GetAgvGraphs"/> request.
    /// </summary>
    /// <exception cref="AgvsNotFoundException"></exception>
    public async Task Consume(ConsumeContext<GetAgvGraphs> context)
    {
        ICollection<Guid> selectedAgvs = context.Message.CorrelationIds;

        ICollection<Domain.Models.Agv.Agv> agvs = await _agvDb.Agvs.SelectExistingAgvsAsync(selectedAgvs.ToList(), context.CancellationToken);
        if (agvs.Count != selectedAgvs.Count)
        {
            throw new AgvsNotFoundException(selectedAgvs.Except(agvs.Select(a => a.CorrelationId)).ToList());
        }

        ICollection<AgvGraph> agvGraphs = await _agvDb.Graphs.GetSelectedAgvGraphsAsync(selectedAgvs, context.CancellationToken);
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
        var graph = await _agvDb.Graphs.GetGraphForMap(correlationId, context.CancellationToken);
        var gotMapGraph = new GotMapGraph(_mapper.Map<Graph>(graph));
        await context.RespondAsync(gotMapGraph);
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<GetGraph> context)
    {
        var graph = await _agvDb.Graphs.GetGraphAsync(context.CancellationToken);
        await context.RespondAsync(new GotGraph(graph));
    }
}