using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Messages.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

/// <summary>
/// Transform station list from AGV into MassTransit message.
/// </summary>
[UsedImplicitly]
public class AgvStationListConsumer : IConsumer<MqttStationList>
{
    private readonly ILogger<AgvStationListConsumer> _logger;

    /// <summary>
    /// Create consumer with message handler delegate.
    /// </summary>
    /// <param name="logger">Mandatory logger facility.</param>
    public AgvStationListConsumer(ILogger<AgvStationListConsumer> logger)
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

        //Special treatment because we are in a different coordinate system than Karis AGV
        foreach (var t in reversedYStationList.Stations)
        {
            t.Position.Y *= -1;
        }

        await context.Publish(new InternalStationListReceived(
            context.Message.SerialNumber.ToGuid(),
            context.Message
        ), context.CancellationToken);
    }
}