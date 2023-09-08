using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using TransportOrder.Domain.Models.ActionGroup;
using TransportOrder.Domain.Repositories;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="ActionGroupInvalid" /> and calls <see cref="IActionHubContext.SendActionGroupStateToAllClients" />
/// </summary>
[PublicAPI]
public class ActionGroupInvalidConsumer : IConsumer<ActionGroupInvalid>
{
    private readonly IActionHubContext _actionHubContext;
    private readonly ILogger<ActionGroupInvalidConsumer> _logger;
    private readonly IActionGroupRetrievalService _actionGroupRetrievalService;
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new <see cref="ActionGroupInvalidConsumer"/> instance.
    /// </summary>
    public ActionGroupInvalidConsumer(IActionHubContext actionHubContext, IActionGroupRetrievalService actionGroupRetrievalService, IMapper mapper,ILogger<ActionGroupInvalidConsumer> logger)
    {
        _actionHubContext = actionHubContext;
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

        var enrichedActionGroup = await _actionGroupRetrievalService.EnrichActionGroupWithTargetNodeTypesAsync(_mapper.Map<ActionGroup>(context.Message.ActionGroup));

        await _actionHubContext.SendActionGroupStateToAllClients(enrichedActionGroup);
    }
}
