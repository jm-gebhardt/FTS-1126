using AutoMapper;
using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Handles ResendAction related request.
/// </summary>
[UsedImplicitly]
public class ResendActionRequestHandler :
    IConsumer<ResendAction>
{
    private readonly ILogger<ResendActionRequestHandler> _logger;
    private readonly ILoggingService _loggingService;
    private readonly IMapper _mapper;
    private readonly IAgvDb _agvDb;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly IBus _bus;

    /// <summary>
    /// Provides handlers to resend the action of an order for the current agv.
    /// </summary>
    /// <inheritdoc cref="ResendActionRequestHandler"/>>
    public ResendActionRequestHandler(ILogger<ResendActionRequestHandler> logger, ILoggingService loggingService, IMapper mapper, IAgvDb agvDb, IActionGroupRepository actionGroupRepository, IBus bus)
    {
        _logger = logger;
        _loggingService = loggingService;
        _mapper = mapper;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
        _bus = bus;
    }
    
    /// <summary>
    /// Handles the <see cref="ResendAction"/> command.
    /// </summary>
    public async Task Consume(ConsumeContext<ResendAction> context)
    {
        (var agvId, string user) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvId, context.CancellationToken);
        var actionGroup = await _actionGroupRepository.GetCurrentAssignedActionGroupAsync(agvId, context.CancellationToken);

        if (actionGroup == null)
        {
            await context.RespondAsync(new ActionNotFound());
            return;
        }

        var action = actionGroup.FirstNotCompletedAction;

        if (action == null)
        {
            await context.RespondAsync(new ActionNotFound());
            return;
        }

        if (action.IsIncomplete)
        {
            throw new NotInCorrectStateException("Cannot resend an incomplete action");
        }

        await _bus.Publish(new Common.Messages.ActionGroups.Commands.ResendAction(agvId, agv.SerialNumber, _mapper.Map<AgvAction>(action)), context.CancellationToken);
        await _loggingService.LogOrder(
            _logger,
            LogLevel.Information, LogCodes.OrderActionResent, agv.CorrelationId, actionGroup.CorrelationId,
            "Order resent", $"{action.ActionType} of order {actionGroup.OrderId} was resent to AGV {agv.SerialNumber}",
            new Dictionary<string, object?>
            {
                { LogLabels.ActionType, action.ActionType },
                { LogLabels.OrderId, actionGroup.OrderId }, 
                { LogLabels.AgvSerialNumber, agv.SerialNumber }, 
                { LogLabels.ActionId, action.CorrelationId }
            }, context.CancellationToken);
        await _loggingService.LogOrder(_logger, LogLevel.Information, LogCodes.OrderOperatorInteraction, actionGroup.AssignedAgv.GetValueOrDefault(), actionGroup.CorrelationId, "Operator Interaction", "Operator interacted with order", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.OrderActionResent }, { LogLabels.ActionId, action.CorrelationId } }, context.CancellationToken);
        await context.RespondAsync(new Success());
    }
}