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
/// Consumes <see cref="ActionGroupInvalid" /> and sends messages via <see cref="IActionGroupPublisher"/>
/// </summary>
[PublicAPI]
public class ActionGroupInvalidConsumer : IConsumer<ActionGroupInvalid>
{
    private readonly IActionGroupPublisher _actionGroupPublisher;
    private readonly ILogger<ActionGroupInvalidConsumer> _logger;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupInvalidConsumer"/> instance.
    /// </summary>
    public ActionGroupInvalidConsumer(IActionGroupPublisher actionGroupPublisher, IActionGroupRetrievalService actionGroupRetrievalService, IMapper mapper, ILogger<ActionGroupInvalidConsumer> logger)
    {
        _actionGroupPublisher = actionGroupPublisher;
        _actionGroupRetrievalService = actionGroupRetrievalService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<ActionGroupInvalid> context)
    {
        _logger.LogDebug("{MessageType} {CorrelationId} received", nameof(ActionGroupInvalid), context.Message.CorrelationId);

        var enrichedActionGroup = await _actionGroupRetrievalService.EnrichActionGroupWithNodesAsync(_mapper.Map<ActionGroup>(context.Message.ActionGroup));
        await _actionGroupPublisher.SendActionGroupChangedAsync(enrichedActionGroup);
    }
}