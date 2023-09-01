using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Repositories;
using TransportOrder.Messages.Events.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumer for <see cref="AgvGraphReceived" />
/// Consumes AgvGraphReceived event and add/updates the AgvGraph in the registry
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class AgvGraphReceivedEventHandler : IConsumer<AgvGraphReceived>
{
    private readonly IAgvGraphRegistry _agvGraphRegistry;
    private readonly ILogger<AgvGraphReceivedEventHandler> _logger;

    /// <inheritdoc cref="AgvGraphReceivedEventHandler" />
    /// <param name="agvGraphRegistry"></param>
    /// <param name="logger"></param>
    public AgvGraphReceivedEventHandler(IAgvGraphRegistry agvGraphRegistry, ILogger<AgvGraphReceivedEventHandler> logger)
    {
        _agvGraphRegistry = agvGraphRegistry;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<AgvGraphReceived> context)
    {
        var agvGraph = context.Message.Graph;
        await context.Publish(new AgvGraphChanged(agvGraph.CorrelationId, agvGraph));

        await _agvGraphRegistry.AddOrUpdateAsync(agvGraph);
        _logger.LogInformation("Graph {GraphCorrelationId} added or updated in registry", agvGraph.CorrelationId);
    }
}