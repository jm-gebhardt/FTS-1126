using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Transform station list from AGV into MassTransit message.
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
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
        /// Consume graph message from AGV and transform this into <see cref="MqttStationListReceived"/> message.
        /// </summary>
        /// <exception cref="InvalidOperationException">Message content is invalid and cannot be processed.</exception>
        public async Task Consume(ConsumeContext<MqttStationList> context)
        {
            _logger.LogInformation("Station list for {AgvId} received", context.Message.SerialNumber);
            await context.Publish(new MqttStationListReceived(
                correlationId: context.Message.SerialNumber.ToGuid(),
                stationList: context.Message
            ), context.CancellationToken);
        }
    }
}