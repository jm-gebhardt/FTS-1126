using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Consumes <see cref="AgvPositionReceived"/> and populates <see cref="AgvPositionRegistry"/>.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvPositionReceivedConsumer : IConsumer<AgvPositionReceived>
    {
        private readonly ILogger<AgvPositionReceivedConsumer> _logger;
        private readonly IAgvPositionRegistry _agvPositionRegistry;


        /// <summary>
        /// Constructor
        /// </summary>
        public AgvPositionReceivedConsumer(ILogger<AgvPositionReceivedConsumer> logger, IAgvPositionRegistry agvPositionRegistry)
        {
            _logger = logger;
            _agvPositionRegistry = agvPositionRegistry;
        }

        /// <summary>
        /// Update AGV position registry on receiving <see cref="AgvPositionReceived"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Consume(ConsumeContext<AgvPositionReceived> context)
        {
            _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(AgvPositionReceived), context.Message.AgvId, context.Message.CorrelationId);
            _agvPositionRegistry.UpdatePosition(context.Message);
            return Task.CompletedTask;
        }
    }
}
