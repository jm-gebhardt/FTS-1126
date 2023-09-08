using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Models.ActionGroup;
using TransportOrder.Domain.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="ActionGroupCreated"/> and calls <see cref="IActionHubContext.SendActionGroupStateToAllClients"/>.
/// </summary>
[JetBrains.Annotations.PublicAPI]
public class ActionGroupCreatedConsumer
    : IConsumer<ActionGroupCreated>
{
    private readonly ILogger<ActionGroupCreatedConsumer> _logger;
    private readonly IActionHubContext _actionHubContext;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupCreatedConsumer"/>.
    /// </summary>
    public ActionGroupCreatedConsumer(
        ILogger<ActionGroupCreatedConsumer> logger,
        IActionHubContext actionHubContext,
        IActionGroupRetrievalService actionGroupRetrievalService,
        IMapper mapper)
    {
        _logger = logger;
        _actionHubContext = actionHubContext;
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

        var actionGroupReadModel = await _actionGroupRetrievalService.EnrichActionGroupWithTargetNodeTypesAsync(_mapper.Map<ActionGroup>(actionGroup));
        await _actionHubContext.SendActionGroupStateToAllClients(actionGroupReadModel);
    }
}
