using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;


namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services
{
    /// <summary>
    /// Transform graph information from AGV into MassTransit message.
    /// </summary>
    [JetBrains.Annotations.UsedImplicitly]
    public class AgvGraphConsumer : IConsumer<MqttGraph>
    {
        private readonly ILogger<AgvGraphConsumer> _logger;


        /// <summary>
        /// Create consumer with message handler delegate.
        /// </summary>
        /// <param name="logger">Mandatory logger facility.</param>
        public AgvGraphConsumer(ILogger<AgvGraphConsumer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Consume graph message from AGV and transform this into <see cref="InternalGraphReceived"/> message.
        /// </summary>
        /// <exception cref="InvalidOperationException">Message content is invalid and cannot be processed.</exception>
        public async Task Consume(ConsumeContext<MqttGraph> context)
        {
            _logger.LogInformation("{MessageType} from AGV {AgvId} received", nameof(MqttGraph), context.Message.SerialNumber);
            var reversedYGraph = context.Message;

            //Special treatment because we are in a different coordinate system than Karis AGV
            if (reversedYGraph.Nodes != null)
            {
                foreach (var t in reversedYGraph.Nodes)
                {
                    t.Y *= -1;
                }
            }
            await context.Publish(new InternalGraphReceived(
                correlationId: context.Message.SerialNumber.ToGuid(),
                graph: reversedYGraph
            ), context.CancellationToken);
        }
    }
}