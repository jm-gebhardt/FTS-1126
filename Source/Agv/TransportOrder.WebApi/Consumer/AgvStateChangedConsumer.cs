using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer
{
    /// <summary>
    /// Consumes <see cref="AgvStateChanged"/> and calls <see cref="IActionHubContext.SendActionGroupStateToAllClients"/>.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvStateChangedConsumer : IConsumer<AgvStateChanged>
    {
        private readonly ILogger<AgvStateChangedConsumer> _logger;
        private readonly IAgvHubContext _agvHubContext;

        /// <summary>
        /// Constructor
        /// </summary>
        public AgvStateChangedConsumer(
            ILogger<AgvStateChangedConsumer> logger,
            IAgvHubContext agvHubContext)
        {
            _logger = logger;
            _agvHubContext = agvHubContext;
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AgvStateChanged> context)
        {
            _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupChanged), context.Message.CorrelationId);

            bool isInitialState = Equals(context.Message.Agv.CurrentState, nameof(AgvStateMachine.Initial));
            await _agvHubContext.SendAgvStateToAllClients(context.Message.Agv, isInitialState);
        }
    }
}
