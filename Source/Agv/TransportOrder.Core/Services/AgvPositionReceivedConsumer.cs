using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

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
        private readonly IGraphRegistry _graphRegistry;
        private readonly IMapper _mapper;

        /// <summary>
        /// Constructor
        /// </summary>
        public AgvPositionReceivedConsumer(
            ILogger<AgvPositionReceivedConsumer> logger,
            IAgvPositionRegistry agvPositionRegistry,
            IGraphRegistry graphRegistry,
            IMapper mapper)
        {
            _logger = logger;
            _agvPositionRegistry = agvPositionRegistry;
            _graphRegistry = graphRegistry;
            _mapper = mapper;
        }

        /// <summary>
        /// Update AGV position registry on receiving <see cref="AgvPositionReceived"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public Task Consume(ConsumeContext<AgvPositionReceived> context)
        {
            _logger.LogTrace(
                "{MessageType} {AgvId} / {CorrelationId} received",
                nameof(AgvPositionReceived),
                context.Message.AgvId,
                context.Message.CorrelationId
            );

            var agvPositionStatus = _mapper.Map<AgvPositionStatus>(context.Message);

            _agvPositionRegistry.UpdatePosition(agvPositionStatus);

            _graphRegistry.UpdateOccupiedNode(agvPositionStatus, out var prevNode, out var currNode);

            if (prevNode == null && currNode != null)
            {
                _logger.LogDebug("Agv {AgvId} arrived at node {NodeId}", context.Message.AgvId, currNode.Id);
            }
            else if (prevNode != null && currNode == null)
            {
                _logger.LogDebug("Agv {AgvId} left node {NodeId}", context.Message.AgvId, prevNode.Id);
            }

            return Task.CompletedTask;
        }
    }
}
