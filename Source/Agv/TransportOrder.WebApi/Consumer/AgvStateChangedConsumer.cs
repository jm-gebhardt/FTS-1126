using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Requests.Agvs.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="AgvStateChanged"/> and sends messages via <see cref="IAgvPublisher"/>
/// </summary>
[PublicAPI]
public class AgvStateChangedConsumer : IConsumer<AgvStateChanged>
{
    private readonly ILogger<AgvStateChangedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly IRequestClient<GetAgvs> _getAgvsClient;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    public AgvStateChangedConsumer(IMapper mapper,
        ILogger<AgvStateChangedConsumer> logger,
        IAgvPublisher agvPublisher,
        IRequestClient<GetAgvs> getAgvsClient)
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
    public async Task Consume(ConsumeContext<AgvStateChanged> context)
    {
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupChanged), context.Message.CorrelationId);

        var agv = context.Message.Agv;
        bool isInitialState = agv.CurrentState == AgvState.Initial;
        await _agvPublisher.SendAgvStateAsync(agv, isInitialState);
    }
}