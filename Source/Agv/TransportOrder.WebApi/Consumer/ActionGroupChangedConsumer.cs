using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="ActionGroupChanged"/> and sends messages via <see cref="IActionGroupPublisher"/>
/// </summary>
[PublicAPI]
public class ActionGroupChangedConsumer : IConsumer<ActionGroupChanged>
{
    private readonly ILogger<ActionGroupChangedConsumer> _logger;
    private readonly IActionGroupPublisher _actionGroupPublisher;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor
    /// </summary>
    public ActionGroupChangedConsumer(
        ILogger<ActionGroupChangedConsumer> logger,
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
    public async Task Consume(ConsumeContext<ActionGroupChanged> context)
    {
        var (actionGroup, correlationId) = context.Message;
        _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupChanged), correlationId);
        var actionGroupReadModel = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(_mapper.Map<ActionGroup>(actionGroup));
        await _actionGroupPublisher.SendActionGroupChangedAsync(actionGroupReadModel);
    }
}