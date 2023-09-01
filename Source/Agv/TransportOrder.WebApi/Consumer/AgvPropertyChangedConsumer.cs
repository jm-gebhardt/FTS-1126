using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TransportOrder.Messages.Events.Agv;
using TransportOrder.Messages.Requests.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvPropertyChanged"/> and sends message via <see cref="IAgvHubContext"/>.
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class AgvPropertyChangedConsumer : IConsumer<AgvPropertyChanged>
{
    private readonly ILogger<AgvPropertyChangedConsumer> _logger;
    private readonly IAgvHubContext _agvHubContext;
    private readonly IRequestClient<GetAgvs> _getAgvsClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    public AgvPropertyChangedConsumer(IMapper mapper,
        ILogger<AgvPropertyChangedConsumer> logger,
        IAgvHubContext agvHubContext,
        IRequestClient<GetAgvs> getAgvsClient
    )
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
    public async Task Consume(ConsumeContext<AgvPropertyChanged> context)
    {
        _logger.LogTrace("{MessageType} {AgvId}/{CorrelationId} received", nameof(AgvPropertyChanged), context.Message.Agv.AgvId, context.Message.Agv.CorrelationId);
            
        Response<GotAgvs> response = await _getAgvsClient.GetResponse<GotAgvs>(new GetAgvs(new[] { context.Message.Agv.CorrelationId }));

        if (!response.Message.Agvs.IsNullOrEmpty())
        {
            var agvDto = _mapper.Map<AgvDto>(response.Message.Agvs.First());
            await _agvHubContext.SendAgvStateToAllClients(agvDto, false);
        }
    }
}