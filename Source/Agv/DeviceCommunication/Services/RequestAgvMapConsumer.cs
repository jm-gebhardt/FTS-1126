using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Consuming a RequestAgvMap message
    /// </summary>
    public class RequestAgvMapConsumer : IConsumer<RequestAgvMap>
    {
        private readonly ILogger<RequestAgvMapConsumer> _logger;

        /// <summary>
        /// Create consumer for agv request map message (<see cref="MqttState"/>)
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        [PublicAPI]
        public RequestAgvMapConsumer(ILogger<RequestAgvMapConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Translate RequestAgvMap content into a MqttRequestMap message
        /// </summary>
        public async Task Consume(ConsumeContext<RequestAgvMap> context)
        {
            await context.PublishMqtt(new MqttRequestMap(context.Message.AgvId, context.Message.CorrelationId), context.CancellationToken);
            _logger.LogInformation("Requesting map {MapId}/{MapCorrelationId} for AGV {AgvId}/{CorrelationId}", 
                context.Message.MapId, context.Message.MapCorrelationId, context.Message.AgvId, context.Message.CorrelationId);
        }
    }
}
