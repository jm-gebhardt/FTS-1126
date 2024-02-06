using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="ActionGroupStarted"/> and sends message via <see cref="IAgvPublisher"/>
/// </summary>
[PublicAPI]
public class ActionGroupStartedConsumer : IConsumer<ActionGroupStarted>
{
    private readonly ILogger<ActionGroupStartedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupStartedConsumer"/>.
    /// </summary>
    public ActionGroupStartedConsumer(
        IAgvPublisher agvPublisher,
        IMapper mapper,
        ILogger<ActionGroupStartedConsumer> logger)
    {
        _agvPublisher = agvPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public Task Consume(ConsumeContext<ActionGroupStarted> context)
    {
        var (actionGroup, correlationId, _) = context.Message;
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupStarted), correlationId);
        return _agvPublisher.SendAssignedActionGroupChangedAsync(actionGroup.AssignedAgv!.Value, actionGroup.CorrelationId);
    }
}