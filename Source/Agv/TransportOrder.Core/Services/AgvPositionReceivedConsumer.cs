using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvPositionStatus = Gebhardt.StoreWare.Agv.TransportOrder.Core.Models.AgvPositionStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services
{
    /// <summary>
    /// Consumes <see cref="AgvPositionReceived"/> and populates <see cref="AgvPositionRegistry"/>.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvPositionReceivedConsumer : IConsumer<AgvPositionReceived>
    {
        private readonly ILogger<AgvPositionReceivedConsumer> _logger;
        private readonly IAgvPositionRegistry _agvPositionRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor
        /// </summary>
        public AgvPositionReceivedConsumer(
            ILogger<AgvPositionReceivedConsumer> logger,
            IAgvPositionRegistry agvPositionRegistry,
            IMapper mapper)
        {
            _logger = logger;
            _agvPositionRegistry = agvPositionRegistry;
            _mapper = mapper;
        }

        /// <summary>
        /// Update AGV position registry on receiving <see cref="AgvPositionReceived"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Consume(ConsumeContext<AgvPositionReceived> context)
        {
            _logger.LogTrace("{MessageType} {AgvId} / {CorrelationId} received", nameof(AgvPositionReceived), context.Message.AgvId, context.Message.CorrelationId);

            var agvPositionStatus = _mapper.Map<AgvPositionStatus>(context.Message);

            _agvPositionRegistry.UpdatePosition(agvPositionStatus);
            return Task.CompletedTask;
        }
    }
}
