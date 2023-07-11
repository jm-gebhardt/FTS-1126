using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer
{
    /// <summary>
    /// Consumes <see cref="ActionGroupCreated"/> and calls <see cref="IActionHubContext.SendActionGroupStateToAllClients"/>.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class ActionGroupCreatedConsumer
        : IConsumer<ActionGroupCreated>
    {
        private readonly ILogger<ActionGroupCreatedConsumer> _logger;
        private readonly IActionHubContext _actionHubContext;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActionGroupCreatedConsumer(
            ILogger<ActionGroupCreatedConsumer> logger,
            IActionHubContext actionHubContext)
        {
            _logger = logger;
            _actionHubContext = actionHubContext;
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<ActionGroupCreated> context)
        {
            _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupCreated), context.Message.CorrelationId);

            await _actionHubContext.SendActionGroupStateToAllClients(context.Message.ActionGroup);
        }
    }
}
