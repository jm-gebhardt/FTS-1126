using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvPositionStatus = Gebhardt.StoreWare.Agv.TransportOrder.Core.Models.AgvPositionStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Consumes <see cref="AgvPositionChanged"/> and sends messages to <see cref="IAgvHubContext"/>.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvPositionChangedConsumer : IConsumer<AgvPositionChanged>
    {
        private readonly ILogger<AgvPositionChangedConsumer> _logger;
        private readonly IAgvHubContext _agvHubContext;
        private readonly IMapper _mapper;

        /// <summary>
        /// </summary>
        public AgvPositionChangedConsumer(
            ILogger<AgvPositionChangedConsumer> logger,
            IAgvHubContext agvHubContext,
            IMapper mapper)
        {
            _logger = logger;
            _agvHubContext = agvHubContext;
            _mapper = mapper;
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Consume(ConsumeContext<AgvPositionChanged> context)
        {
            _logger.LogTrace("{MessageType} {AgvId}/{CorrelationId} received", nameof(AgvPositionChanged), context.Message.AgvId, context.Message.CorrelationId);

            var agvPositionStatus = _mapper.Map<AgvPositionStatus>(context.Message);

            await _agvHubContext.SendAgvPositionToAllClients(nameof(Defines.HubMethods.AgvPositionChanged), agvPositionStatus, context.CancellationToken);
        }
    }
}
