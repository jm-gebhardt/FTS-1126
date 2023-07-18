using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Graphs;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Actions;

/// <summary>
/// Consumer for <see cref="AgvGraphChanged" />
/// Consumes AgvGraphChanged event and add/updates the AgvGraph in the registry
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class AgvGraphReceivedConsumer : IConsumer<AgvGraphReceived>
{
    private readonly IAgvGraphRegistry _agvGraphRegistry;
    private readonly ILogger<AgvGraphReceivedConsumer> _logger;

    /// <inheritdoc cref="AgvGraphReceivedConsumer" />
    /// <param name="agvGraphRegistry"></param>
    /// <param name="logger"></param>
    public AgvGraphReceivedConsumer(IAgvGraphRegistry agvGraphRegistry, ILogger<AgvGraphReceivedConsumer> logger)
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