using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Simulate finishing of host orders
    /// </summary>
    public class AgvStateConsumer : IConsumer<IState>
    {
        private readonly ILogger<AgvStateConsumer> _logger;

        /// <summary>
        /// Create consumer with message handler delegate for simulation.
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        [PublicAPI]
        public AgvStateConsumer(ILogger<AgvStateConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// <para>Logs received message and then discards it.</para>
        /// </summary>
        /// <param name="context">Context of the consumer wrapping details surrounding the inbound message.</param>
        public async Task Consume(ConsumeContext<IState> context)
        {
            await Task.Run(() => // Await Task to satisfy async
            {
                _logger.LogInformation("AGV State handling succeeded for OrderId {OrderId} at {Timestamp}", context.Message.OrderId, context.Message.Timestamp);
            });
        }
    }
}
