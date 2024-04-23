using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="ActionGroupCreated"/> and sends messages via <see cref="IActionGroupPublisher"/>
/// </summary>
[PublicAPI]
public class ActionGroupCreatedConsumer : IConsumer<ActionGroupCreated>
{
    private readonly ILogger<ActionGroupCreatedConsumer> _logger;
    private readonly IActionGroupPublisher _actionGroupPublisher;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupCreatedConsumer"/>.
    /// </summary>
    public ActionGroupCreatedConsumer(
        ILogger<ActionGroupCreatedConsumer> logger,
        IActionGroupPublisher actionGroupPublisher,
        IActionGroupRetrievalService actionGroupRetrievalService,
        IMapper mapper)
    {
        _logger = logger;
        _actionGroupPublisher = actionGroupPublisher;
        _actionGroupRetrievalService = actionGroupRetrievalService;
        _mapper = mapper;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<ActionGroupCreated> context)
    {
        var (actionGroup, correlationId) = context.Message;
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupCreated), correlationId);
        var actionGroupReadModel = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(_mapper.Map<ActionGroup>(actionGroup));
        await _actionGroupPublisher.SendActionGroupCreatedAsync(actionGroupReadModel);
    }
}