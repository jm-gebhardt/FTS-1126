using Gebhardt.Acc.DeviceCommunication.SimAgv.Commands;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;

/// <summary>
/// Receive <see cref="MqttSimulateAgvError" />
/// </summary>
[UsedImplicitly]
public class MqttSimulateAgvErrorConsumer : IConsumer<MqttSimulateAgvError>
{
    private readonly ILogger<MqttSimulateAgvErrorConsumer> _logger;

    /// <inheritdoc cref="MqttSimulateAgvErrorConsumer" />
    /// <param name="logger"></param>
    public MqttSimulateAgvErrorConsumer(ILogger<MqttSimulateAgvErrorConsumer> logger)
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