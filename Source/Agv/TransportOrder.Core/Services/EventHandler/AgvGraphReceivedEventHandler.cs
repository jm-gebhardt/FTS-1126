using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
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

    /// <inheritdoc cref="AgvGraphReceivedEventHandler" />
    public AgvGraphReceivedEventHandler(IGraphRepository graphRepository,
        IMapRepository mapRepository,
        ILogger<AgvGraphReceivedEventHandler> logger)
    {
        _graphRepository = graphRepository;
        _mapRepository = mapRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvGraphReceived> context)
    {
        var agvGraph = context.Message.Graph;

        var result = await _graphRepository.AddOrUpdateAgvGraphAsync(agvGraph);
        if (result.AnyRelationsMissing)
        {
            _logger.LogInformation("Graph {GraphCorrelationId} was not stored in the repository. Emitting GraphMissing event", agvGraph.AgvCorrelationId);
            await context.Publish(new GraphMissing(agvGraph.AgvCorrelationId), context.CancellationToken);
            return;
        }

        await context.Publish(new SetAgvGraphExist(agvGraph.AgvCorrelationId, context.Message.AgvTypeIdentifier));

        if (result.AnyChanges)
        {
            await context.Publish(new AgvGraphChanged(agvGraph.AgvCorrelationId));
            _logger.LogInformation("Graph {GraphCorrelationId} added or updated in repository", agvGraph.AgvCorrelationId);
        }
    }
}