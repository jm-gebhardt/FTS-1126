using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using MassTransit;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// Initializes a new <see cref="ActionGroupCreatedConsumer"/>.
    /// </summary>
    public ActionGroupCreatedConsumer(
        ILogger<ActionGroupCreatedConsumer> logger,
        IActionHubContext actionHubContext, IActionGroupRetrievalService actionGroupRetrievalService)
    {
        _logger = logger;
        _actionHubContext = actionHubContext;
        _actionGroupRetrievalService = actionGroupRetrievalService;
    }

    /// <summary>
    /// Send message to all clients
    /// </summary>
    /// <param name="context"></param>
    public async Task Consume(ConsumeContext<ActionGroupCreated> context)
    {
        var (actionGroup, correlationId) = context.Message;
            _logger.LogTrace("{MessageType} {CorrelationId} received", nameof(ActionGroupCreated), correlationId);

        var actionGroupReadModel = await _actionGroupRetrievalService.EnrichActionGroupWithTargetNodeTypesAsync(actionGroup);
        await _actionHubContext.SendActionGroupStateToAllClients(actionGroupReadModel);
    }
}
