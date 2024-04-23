using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// Consumer for <see cref="AgvDataReceived"/> messages
/// </summary>
[PublicAPI]
public class AgvDataEventHandler : IConsumer<AgvDataReceived>
{
    private readonly ILogger<AgvDataEventHandler> _logger;

    /// <summary>
    /// Consumer for <see cref="AgvDataReceived"/> messages
    /// </summary>
    public AgvDataEventHandler(ILogger<AgvDataEventHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles the <see cref="AgvDataReceived"/> event.
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<AgvDataReceived> context)
    {
        _logger.LogInformation("Received AgvData for {AgvId} / {CorrelationId}", context.Message.SerialNumber, context.Message.CorrelationId);

        await context.Publish(new UpdateAgvData(context.Message.CorrelationId, context.Message.Data));
    }
}