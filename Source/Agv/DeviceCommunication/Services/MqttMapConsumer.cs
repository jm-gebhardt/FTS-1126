using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
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
            if (!DateTime.TryParse(context.Message.ModificationDate, null, System.Globalization.DateTimeStyles.RoundtripKind, out var modificationDate))
            {
                _logger.LogError("Invalid datetime format for modification date: {Status}", context.Message);
                throw new FormatException($"Cannot handle {typeof(MqttMap)} message, modification date need to be in ISO8601 format");
            }

            if (context.Message.MapId == null)
            {
                _logger.LogError("Incoming Map cannot be forwarded: MapId is NULL. Message: {Message}", context.Message);
            }
            else if (context.Message.Data == null)
            {
                _logger.LogError("Incoming Map cannot be forwarded: PNG string is NULL. Message: {Message}", context.Message);
            } 
            else
            {
                await context.Publish(new AgvMapReceived(
                    context.Message.SerialNumber.ToGuid(),
                    context.Message.SerialNumber,
                    new Common.Messages.AgvManagement.AgvMap(
                        context.Message.MapId.ToGuid(),
                        context.Message.MapId,
                        modificationDate,
                        context.Message.Data,
                        context.Message.Resolution,
                        context.Message.XOffset,
                        context.Message.YOffset
                    )
                ));
            }
        }
    }
}
