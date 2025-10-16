using AutoMapper;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Models.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Models.LogEntry;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.ActionGroup;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.WebApi.Consumer;

/// <summary>
/// Consumes <see cref="ActionGroupChanged"/> and sends messages via <see cref="IActionGroupPublisher"/>
/// </summary>
[PublicAPI]
public class ActionGroupChangedConsumer : 
    IConsumer<ActionGroupChanged>,
    IConsumer<LogEntryCreated>
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

    /// <inheritdoc cref="ActionGroupChangedConsumer"/>>
    public async Task Consume(ConsumeContext<LogEntryCreated> context)
    {
        var logEntry = context.Message.LogEntry;

        if (logEntry.Category == Categories.Order)
        {
            await _actionGroupPublisher.SendLogEntryCreated(logEntry.ActionGroupCorrelationId.GetValueOrDefault(), _mapper.Map<LogEntry>(logEntry));
        }
    }
}