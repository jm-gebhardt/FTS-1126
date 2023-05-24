using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs
{
    /// <summary>
    /// Consumes <see cref="AgvPropertyChanged"/> and sends message of type <see cref="Defines.HubMethods.AgvChanged"/> via <see cref="IAgvHubContext"/>.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvPropertyChangedConsumer : IConsumer<AgvPropertyChanged>
    {
        private readonly ILogger<AgvPropertyChangedConsumer> _logger;
        private readonly IAgvHubContext _agvHubContext;


        /// <summary>
        /// Constructor
        /// </summary>
        public AgvPropertyChangedConsumer(
            ILogger<AgvPropertyChangedConsumer> logger,
            IAgvHubContext agvHubContext)
        {
            _logger = logger;
            _agvHubContext = agvHubContext;
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AgvPropertyChanged> context)
        {
            _logger.LogTrace("{MessageType} {AgvId}/{CorrelationId} received", nameof(AgvPropertyChanged), context.Message.Agv.AgvId, context.Message.Agv.CorrelationId);

            await _agvHubContext.SendAgvStateToAllClients(nameof(Defines.HubMethods.AgvChanged), context.Message.Agv, context.CancellationToken);
        }
    }
}
