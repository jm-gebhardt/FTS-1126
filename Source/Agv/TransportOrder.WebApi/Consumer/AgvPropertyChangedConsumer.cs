using AutoMapper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvPropertyChanged"/> and sends message via <see cref="IAgvPublisher"/>.
/// </summary>
[PublicAPI]
public class AgvPropertyChangedConsumer : IConsumer<AgvPropertyChanged>
{
    private readonly ILogger<AgvPropertyChangedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly IRequestClient<GetAgvs> _getAgvsClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    public AgvPropertyChangedConsumer(IMapper mapper,
        ILogger<AgvPropertyChangedConsumer> logger,
        IAgvPublisher agvPublisher,
        IRequestClient<GetAgvs> getAgvsClient
    )
    {
        _mapper = mapper;
        _logger = logger;
        _agvPublisher = agvPublisher;
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

        if (response.Message.Agvs.Any())
        {
            await _agvPublisher.SendAgvStateAsync(response.Message.Agvs.First(), false);
        }
    }
}