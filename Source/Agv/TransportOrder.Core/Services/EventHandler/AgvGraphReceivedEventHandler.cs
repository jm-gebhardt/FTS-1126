using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumer for <see cref="AgvGraphReceived" />
/// Consumes AgvGraphReceived event and add/updates the AgvGraph in the repository
/// </summary>
[PublicAPI]
public class AgvGraphReceivedEventHandler : IConsumer<AgvGraphReceived>
{
    private readonly IGraphRepository _graphRepository;
    private readonly IMapRepository _mapRepository;
    private readonly ILogger<AgvGraphReceivedEventHandler> _logger;
    private readonly IStationRepository _stationRepository;

    /// <inheritdoc cref="AgvGraphReceivedEventHandler" />
    public AgvGraphReceivedEventHandler(IGraphRepository graphRepository,
        IMapRepository mapRepository,
        IStationRepository stationRepository,
        ILogger<AgvGraphReceivedEventHandler> logger)
    {
        _graphRepository = graphRepository;
        _stationRepository = stationRepository;
        _mapRepository = mapRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvGraphReceived> context)
    {
        var agvGraph = context.Message.Graph;

        if (await _graphRepository.AddOrUpdateAgvGraphAsync(agvGraph))
        {
            await context.Publish(new AgvGraphChanged(agvGraph.CorrelationId));
            await _stationRepository.AddStations(agvGraph);

            _logger.LogInformation("Graph {GraphCorrelationId} added or updated in repository.", agvGraph.CorrelationId);
        }
        else
        {
            _logger.LogInformation("Graph {GraphCorrelationId} was not stored in the repository. Emitting GraphMissing event.", agvGraph.CorrelationId);
            await context.Publish(new GraphMissing(agvGraph.CorrelationId), context.CancellationToken);
        }
    }
}