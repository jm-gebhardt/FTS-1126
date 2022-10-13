using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Web;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services
{
    /// <summary>
    /// Consumes <see cref="AgvPositionChanged"/> and sends messages to <see cref="IAgvHubContext"/>.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvPositionChangedConsumer : IConsumer<AgvPositionChanged>
    {
        private readonly ILogger<AgvPositionReceivedConsumer> _logger;
        private readonly IAgvHubContext _agvHubContext;

        /// <summary>
        /// </summary>
        public AgvPositionChangedConsumer(
            ILogger<AgvPositionReceivedConsumer> logger,
            IAgvHubContext agvHubContext)
        {
            _logger = logger;
            _agvHubContext = agvHubContext;
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<AgvPositionChanged> context)
        {
            _logger.LogTrace("{MessageType} {AgvId} received", nameof(AgvPositionChanged), context.Message.AgvId);

            var agvPositionChanged = context.Message;
            await _agvHubContext.SendAgvPositionToAllClients(nameof(Defines.HubMethods.AgvPositionChanged), new AgvPositionReceived(agvPositionChanged.CorrelationId, agvPositionChanged.AgvId, agvPositionChanged.NewAgvPosition), context.CancellationToken);
        }
    }
}
