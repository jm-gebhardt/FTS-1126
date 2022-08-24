using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Handle data information from AGV.
    /// </summary>
    /// <remarks>
    /// This is passed to the service AgvManagement to have kept DeviceCommunication as a simple proxy.
    /// </remarks>
    [JetBrains.Annotations.UsedImplicitly]
    public class AgvDataConsumer : IConsumer<MqttAgvData>
    {
        private readonly ILogger<AgvDataConsumer> _logger;

        /// <summary>
        /// Create consumer with message handler delegate.
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        public AgvDataConsumer(ILogger<AgvDataConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Consume data message from AGV and transform this to an internal <see cref="AgvDataReceived"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Message content is invalid and cannot be processed.</exception>
        public async Task Consume(ConsumeContext<MqttAgvData> context)
        {
            var parser = new MessageParsers.AgvDataParser(context.Message);
            _logger.LogInformation("Incoming AgvData: {AgvData} / {CorrelationId}", context.Message.SerialNumber, context.Message.RequestId);

            try
            {
                await context.Publish(new AgvDataReceived(
                    parser.SerialNumber.ToGuid(),
                    parser.SerialNumber,
                    parser.AgvData)
                , context.CancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Message {MessageType} has invalid content: {AgvData} / {CorrelationId}", nameof(MqttAgvData), context.Message, context.CorrelationId);
                throw;
            }
        }
    }
}
