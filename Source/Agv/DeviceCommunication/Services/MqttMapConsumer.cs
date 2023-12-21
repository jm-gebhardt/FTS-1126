using System.Globalization;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Helpers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

/// <summary>
/// Consuming MqttRequestMap messages
/// </summary>
public class MqttMapConsumer : IConsumer<MqttMap>
{
    private readonly ILogger<MqttMapConsumer> _logger;

    /// <summary>
    /// Create consumer for MQTT message responding to a map request (<see cref="MqttMap"/>)
    /// </summary>
    /// <param name="logger">Mandatory logger facility.</param>
    [PublicAPI]
    public MqttMapConsumer(ILogger<MqttMapConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Translate content of MqttMap into an AgvMapReceived message
    /// </summary>
    public async Task Consume(ConsumeContext<MqttMap> context)
    {
        if (string.IsNullOrEmpty(context.Message.SerialNumber))
        {
            _logger.LogError("Missing serialNumber in MqttMap message: {Status}", context.Message);
            throw new InvalidOperationException($"Cannot handle {typeof(MqttMap)} message, serialNumber must not be null or empty");
        }
        if (!DateTime.TryParse(context.Message.ModificationDate, null, DateTimeStyles.RoundtripKind, out var modificationDate))
        {
            _logger.LogError("Invalid datetime format for modification date: {Status}", context.Message);
            throw new FormatException($"Cannot handle {typeof(MqttMap)} message, modification date need to be in ISO8601 format");
        }

        if (context.Message.MapId == null)
        {
            _logger.LogError("Incoming Map cannot be forwarded: MapId is NULL. Message: {Message}", context.Message);
        }
        else
        {
            string? messageData = context.Message.Data;
            if (messageData == null)
            {
                _logger.LogError("Incoming Map cannot be forwarded: PNG string is NULL. Message: {Message}", context.Message);
            }
            else
            {
                var dimensions = MapHelper.GetMapDimensions(Convert.FromBase64String(messageData));

                await context.Publish(new AgvMapReceived(
                    context.Message.SerialNumber.ToGuid(),
                    context.Message.SerialNumber,
                    new Map(
                        context.Message.MapId.ToGuid(),
                        context.Message.MapId,
                        modificationDate,
                        messageData,
                        dimensions.Width,
                        dimensions.Height,
                        context.Message.Resolution,
                        context.Message.XOffset * -1.0f,
                        context.Message.YOffset * -1.0f
                    )
                ));
            }
        }
    }
}