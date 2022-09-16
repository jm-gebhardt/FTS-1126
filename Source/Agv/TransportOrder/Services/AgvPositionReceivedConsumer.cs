using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Web;

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
        private readonly IAgvHubContext _agvHubContext;

        /// <summary>
        /// Constructor
        /// </summary>
        public AgvPositionReceivedConsumer(
            ILogger<AgvPositionReceivedConsumer> logger,
            IAgvPositionRegistry agvPositionRegistry,
            IAgvHubContext agvHubContext)
        {
            _logger = logger;
            _agvPositionRegistry = agvPositionRegistry;
            _agvHubContext = agvHubContext;
        }

        /// <summary>
        /// Update AGV position registry on receiving <see cref="AgvPositionReceived"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<AgvPositionReceived> context)
        {
            _logger.LogDebug("{MessageType} {AgvId} / {CorrelationId} received", nameof(AgvPositionReceived), context.Message.AgvId, context.Message.CorrelationId);
            _agvPositionRegistry.UpdatePosition(context.Message);
            await _agvHubContext.SendAgvPositionToAllClients(nameof(Defines.HubMethods.AgvPositionChanged), context.Message, context.CancellationToken);
        }
    }
}
