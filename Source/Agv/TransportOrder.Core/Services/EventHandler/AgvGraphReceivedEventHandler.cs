using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumer for <see cref="AgvGraphReceived" />
/// Consumes AgvGraphReceived event and add/updates the AgvGraph in the repository
/// </summary>
[PublicAPI]
public class AgvGraphReceivedEventHandler : IConsumer<AgvGraphReceived>
{
    private readonly IAgvGraphRepository _agvGraphRepository;
    private readonly ILogger<AgvGraphReceivedEventHandler> _logger;

    /// <inheritdoc cref="AgvGraphReceivedEventHandler" />
    /// <param name="agvGraphRepository"></param>
    /// <param name="logger"></param>
    public AgvGraphReceivedEventHandler(IAgvGraphRepository agvGraphRepository, ILogger<AgvGraphReceivedEventHandler> logger)
    {
        _agvGraphRepository = agvGraphRepository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvGraphReceived> context)
    {
        var agvGraph = context.Message.Graph;

        await _agvGraphRepository.AddOrUpdateAsync(agvGraph);

        await context.Publish(new AgvGraphChanged(agvGraph.CorrelationId, agvGraph));

        _logger.LogInformation("Graph {GraphCorrelationId} added or updated in repository", agvGraph.CorrelationId);
    }
}