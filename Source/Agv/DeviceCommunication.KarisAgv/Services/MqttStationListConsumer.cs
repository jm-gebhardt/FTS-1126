using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Events;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;

/// <summary>
/// Transform station list from AGV into MassTransit message.
/// </summary>
[UsedImplicitly]
public class MqttStationListConsumer : IConsumer<MqttStationList>
{
    private readonly ILogger<MqttStationListConsumer> _logger;

    /// <summary>
    /// Create consumer with message handler delegate.
    /// </summary>
    /// <param name="logger">Mandatory logger facility.</param>
    public MqttStationListConsumer(ILogger<MqttStationListConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Consume graph message from AGV and transform this into <see cref="InternalStationListReceived"/> message.
    /// </summary>
    /// <exception cref="InvalidOperationException">Message content is invalid and cannot be processed.</exception>
    public async Task Consume(ConsumeContext<MqttStationList> context)
    {
        _logger.LogInformation("{MessageType} from {AgvId} received", nameof(MqttStationList), context.Message.SerialNumber);
        var reversedYStationList = context.Message;

        // Special treatment because we are in a different coordinate system than Karis AGV
        // Karis Station 0 rad is PI/2 rad in our world 
        foreach (var t in reversedYStationList.Stations)
        {
            t.Position.Y *= -1;
            t.Position.Theta = (float)Mathematics.ConvertToRadians(90.0f) - t.Position.Theta;
        }

        await context.Publish(new InternalStationListReceived(
            context.Message.SerialNumber.ToGuid(),
            context.Message
        ), context.CancellationToken);
    }
}