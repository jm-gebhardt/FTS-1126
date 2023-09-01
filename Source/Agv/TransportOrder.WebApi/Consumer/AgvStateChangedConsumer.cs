using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.SagaStateMachines;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvStateChanged"/> and calls <see cref="IActionHubContext.SendActionGroupStateToAllClients"/>.
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class AgvStateChangedConsumer : IConsumer<AgvStateChanged>
{
    private readonly ILogger<AgvStateChangedConsumer> _logger;
    private readonly IAgvHubContext _agvHubContext;
    private readonly IRequestClient<GetAgvs> _getAgvsClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    public AgvStateChangedConsumer(IMapper mapper,
        ILogger<AgvStateChangedConsumer> logger,
        IAgvHubContext agvHubContext,
        IRequestClient<GetAgvs> getAgvsClient)
    {
        _mapper = mapper;
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
            
        Response<GotAgvs> response = await _getAgvsClient.GetResponse<GotAgvs>(new GetAgvs(new[] { context.Message.Agv.CorrelationId }));
            
        if (!response.Message.Agvs.IsNullOrEmpty())
        {
            bool isInitialState = Equals(context.Message.Agv.CurrentState, nameof(AgvStateMachine.Initial));
            var agvDto = _mapper.Map<AgvDto>(response.Message.Agvs.First());
            await _agvHubContext.SendAgvStateToAllClients(agvDto, isInitialState);
        }
    }
}