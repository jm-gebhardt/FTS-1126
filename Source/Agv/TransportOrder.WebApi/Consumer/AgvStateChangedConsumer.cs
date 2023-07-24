using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

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
        private readonly IRequestClient<GetAgvsRequest> _getAgvsClient;

        /// <summary>
        /// Constructor
        /// </summary>
        public AgvStateChangedConsumer(
            ILogger<AgvStateChangedConsumer> logger,
            IAgvHubContext agvHubContext,
            IRequestClient<GetAgvsRequest> getAgvsClient)
        {
            _logger = logger;
            _agvHubContext = agvHubContext;
            _getAgvsClient = getAgvsClient;
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AgvStateChanged> context)
        {
            _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupChanged), context.Message.CorrelationId);
            
            Response<GetAgvsResult> response = await _getAgvsClient.GetResponse<GetAgvsResult>(new GetAgvsRequest(new[] { context.Message.Agv.CorrelationId }));
            
            if (!response.Message.Agvs.IsNullOrEmpty())
            {
                bool isInitialState = Equals(context.Message.Agv.CurrentState, nameof(AgvStateMachine.Initial));
                await _agvHubContext.SendAgvStateToAllClients(response.Message.Agvs.First(), isInitialState);
            }
        }
    }
}
