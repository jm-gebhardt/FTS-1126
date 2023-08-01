using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Common;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Agvs.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer
{
    /// <summary>
    /// Consumes <see cref="AgvPropertyChanged"/> and sends message via <see cref="IAgvHubContext"/>.
    /// </summary>
    [JetBrains.Annotations.PublicAPI]
    public class AgvPropertyChangedConsumer : IConsumer<AgvPropertyChanged>
    {
        private readonly ILogger<AgvPropertyChangedConsumer> _logger;
        private readonly IAgvHubContext _agvHubContext;
        private readonly IRequestClient<GetAgvsDtoRequest> _getAgvsClient;

        /// <summary>
        /// Constructor
        /// </summary>
        public AgvPropertyChangedConsumer(
            ILogger<AgvPropertyChangedConsumer> logger,
            IAgvHubContext agvHubContext,
            IRequestClient<GetAgvsDtoRequest> getAgvsClient
            )
        {
            _logger = logger;
            _agvHubContext = agvHubContext;
            _getAgvsClient = getAgvsClient;
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <param name="context"></param>
        public async Task Consume(ConsumeContext<AgvPropertyChanged> context)
        {
            _logger.LogTrace("{MessageType} {AgvId}/{CorrelationId} received", nameof(AgvPropertyChanged), context.Message.Agv.AgvId, context.Message.Agv.CorrelationId);
            
            Response<GetAgvsDtoResult> response = await _getAgvsClient.GetResponse<GetAgvsDtoResult>(new GetAgvsDtoRequest(new[] { context.Message.Agv.CorrelationId }));

            if (!response.Message.Agvs.IsNullOrEmpty())
            {
                await _agvHubContext.SendAgvStateToAllClients(response.Message.Agvs.First(), false);
            }
        }
    }
}
