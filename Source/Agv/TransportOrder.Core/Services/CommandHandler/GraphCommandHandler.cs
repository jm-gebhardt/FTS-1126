using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Map;
using Gebhardt.Acc.TransportOrder.Messages.Events;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.CommandHandler;

/// <summary>
/// Consumer for <see cref="UpdateGraphRequest"/>.
/// </summary>
[UsedImplicitly]
public class GraphCommandHandler : 
    IConsumer<UpdateGraphRequest>,
    IConsumer<AgvGraphChanged>,
    IConsumer<AgvTypeGraphUpdated>,
    IConsumer<AgvTypeGraphDeleted>
{
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<GraphCommandHandler> _logger;
    private readonly ILoggingService _loggingService;
    private readonly IStationRepository _stationRepository;

    /// <inheritdoc cref="GraphCommandHandler"/>
    public GraphCommandHandler(IGraphRepository graphRepository, IStationRepository stationRepository, ILogger<GraphCommandHandler> logger, ILoggingService loggingService)
    {
        _graphRepository = graphRepository;
        _logger = logger;
        _loggingService = loggingService;
        _stationRepository = stationRepository;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<UpdateGraphRequest> context)
    {
        _logger.LogInformation("Update Graph");

        var graph = context.Message.Graph;
        graph.Validate();

        await _graphRepository.UpdateGraphAsync(graph);

        if (context.Message.Maps != null)
        {
            await context.Publish(new SaveMaps(context.Message.Maps, context.Message.CorrelationId));
        }
       
        _logger.LogInformation("Graph {CorrelationId} updated", context.Message.CorrelationId);
        await context.Publish(new GraphUpdated(context.Message.CorrelationId, graph, context.Message.Maps), context.CancellationToken);
        await context.RespondAsync(new Success());
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvGraphChanged> context)
    {
        if(!await _graphRepository.ConsolidateGraphAsync())
        {
           await _loggingService.LogAgv(_logger, LogLevel.Error, LogCodes.AgvConsolidatingGlobalGraph, context.Message.CorrelationId, string.Empty, "AGV Graph Consolidation Failed", "AGV Graph Consolidation Failed");
           return;
        }
        await context.Publish(new GlobalGraphConsolidated());
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvTypeGraphUpdated> context)
    {
        if (!await _graphRepository.UpdateAgvTypeGraphsAsync(context.Message.UpdatedTypeGraphs))
        {
            _logger.LogError("Updating agv type graphs failed");
            return;
        }

        foreach (var graph in context.Message.UpdatedTypeGraphs)
        {
            await _stationRepository.AddStations(graph);
        }
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<AgvTypeGraphDeleted> context)
    {
        if (!await _graphRepository.DeleteAgvTypeGraphsAsync(context.Message.UpdatedTypeGraphs))
        {
            _logger.LogError("Deleting agv type graphs failed");
            await context.RespondAsync(new Failed());
            return;
        }
        await context.RespondAsync(new Success());
    }
}