using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumer for <see cref="AgvDataReceived"/> messages
/// </summary>
[PublicAPI]
public class AgvDataEventHandler : IConsumer<AgvDataReceived>
{
    private readonly ILogger<AgvDataEventHandler> _logger;
    private readonly IAgvDataRegistry _agvDataRegistry;

    /// <summary>
    /// Consumer for <see cref="AgvDataReceived"/> messages
    /// </summary>
    public AgvDataEventHandler(ILogger<AgvDataEventHandler> logger, IAgvDataRegistry agvDataRegistry)
    {
        _logger = logger;
        _agvDataRegistry = agvDataRegistry;
    }

    /// <summary>
    /// Handle AgvDataReceived message
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<AgvDataReceived> context)
    {
        _logger.LogInformation("Received AgvData for {AgvId} / {CorrelationId}", context.Message.AgvId, context.Message.CorrelationId);

        if (!await _agvDataRegistry.AddOrUpdateAsync(context.Message.Data))
        {
            _logger.LogWarning("Failed to store AgvData for {AgvId} / {CorrelationId}", context.Message.AgvId, context.Message.CorrelationId);
        }
    }
}