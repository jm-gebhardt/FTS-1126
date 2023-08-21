using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <summary>
/// Consumer for <see cref="AgvDataReceived"/> messages
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class AgvDataConsumer : IConsumer<AgvDataReceived>
{
    private readonly ILogger<AgvDataConsumer> _logger;
    private readonly IAgvDataRegistry _agvDataRegistry;

    /// <summary>
    /// Consumer for <see cref="AgvDataReceived"/> messages
    /// </summary>
    public AgvDataConsumer(ILogger<AgvDataConsumer> logger, IAgvDataRegistry agvDataRegistry)
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

        if (!_agvDataRegistry.AddOrUpdate(context.Message.Data))
        {
            _logger.LogWarning("Failed to store AgvData for {AgvId} / {CorrelationId}", context.Message.AgvId, context.Message.CorrelationId);
        }

        await Task.CompletedTask;
    }
}