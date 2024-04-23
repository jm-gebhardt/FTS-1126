using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="ActionGroupCompleted"/> and sends message via <see cref="IAgvPublisher"/>
/// </summary>
[PublicAPI]
public class ActionGroupCompletedConsumer : IConsumer<ActionGroupCompleted>
{
    private readonly ILogger<ActionGroupCompletedConsumer> _logger;
    private readonly IAgvPublisher _agvPublisher;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupCompletedConsumer"/>.
    /// </summary>
    public ActionGroupCompletedConsumer(
        IAgvPublisher agvPublisher,
        IMapper mapper,
        ILogger<ActionGroupCompletedConsumer> logger)
    {
        _agvPublisher = agvPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public Task Consume(ConsumeContext<ActionGroupCompleted> context)
    {
        var (actionGroup, correlationId, _) = context.Message;
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupCompleted), correlationId);
        return _agvPublisher.SendAssignedActionGroupChangedAsync((Guid)actionGroup.AssignedAgv!, null);
    }
}