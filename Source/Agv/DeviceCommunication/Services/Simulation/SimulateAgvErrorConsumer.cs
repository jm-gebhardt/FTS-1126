using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages.SimulateAgv;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Messages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;

/// <summary>
/// Receive <see cref="MqttSimulateAgvError" />
/// </summary>
[UsedImplicitly]
public class SimulateAgvErrorConsumer : IConsumer<MqttSimulateAgvError>
{
    private readonly ILogger<SimulateAgvErrorConsumer> _logger;

    /// <inheritdoc cref="SimulateAgvErrorConsumer" />
    /// <param name="logger"></param>
    public SimulateAgvErrorConsumer(ILogger<SimulateAgvErrorConsumer> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Create consumer for <see cref="MqttSimulateAgvError" /> and translate message to <see cref="SimulateAgvError" /> message
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<MqttSimulateAgvError> context)
    {
        _logger.LogDebug("{Message} received.", nameof(MqttSimulateAgvError));
        await context.Publish(new SimulateAgvError(context.Message.CorrelationId, context.Message.AgvErrors));
    }
}