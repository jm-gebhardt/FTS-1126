using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Constants;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using MassTransit;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Provides handlers for setting agv modes.
/// </summary>
[UsedImplicitly]
public class AgvModeRequestHandler:
    IConsumer<SetMaintenanceMode>,
    IConsumer<SetNormalMode>,
    IConsumer<SetUserMode>
{
    private readonly IBus _bus;
    private readonly IAgvDb _agvDb;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly ILogger<AgvModeRequestHandler> _logger;

    /// <inheritdoc cref="AgvModeRequestHandler"/>
    public AgvModeRequestHandler(ILogger<AgvModeRequestHandler> logger, IBus bus, IAgvDb agvDb, IActionGroupRepository actionGroupRepository)
    {
        _logger = logger;
        _bus = bus;
        _agvDb = agvDb;
        _actionGroupRepository = actionGroupRepository;
    }

    /// <summary>
    /// Handles the <see cref="SetMaintenanceMode"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<SetMaintenanceMode> context)
    {
        (var agvCorrelationId, string? initiatingUser, var maintenanceReason, string? reasonDescription, var assignedActionPolicy, var currentActionPolicy, bool cancelManualActions) = context.Message;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        await _bus.Publish(new SetMaintenanceMode(agv.CorrelationId, initiatingUser, maintenanceReason, reasonDescription, assignedActionPolicy, currentActionPolicy, cancelManualActions), context.CancellationToken);

        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="SetNormalMode"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<SetNormalMode> context)
    {
        (var agvCorrelationId, string? initiatingUser) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        var pausedActionGroups = await _actionGroupRepository.GetActionGroupsAsync(actionGroupState: ActionGroupState.Paused, assignedAgvCorrelationId: agvCorrelationId, maxItems: 100);

        if (pausedActionGroups.Count == 0)
        {
            _logger.LogDebug("Triggering set normal mode for agv {AgvCorrelationId}", agvCorrelationId);
            await _bus.Publish(new SetNormalMode(agv.CorrelationId, initiatingUser));
            await context.RespondAsync(new Success());
            return;
        }

        foreach (var actionGroup in pausedActionGroups)
        {
            _logger.LogDebug("Triggering resuming of action group {ActionGroupCorrelationId} for agv {AgvCorrelationId}", actionGroup.CorrelationId, agvCorrelationId);
            if (actionGroup.Reasons.Any(r => r.Template == Reasons.LockedStation))
            {
                // We do not allow to set AGV back to normal mode when one target station is locked
                throw new SetAgvToNormalModeFailedException(agvCorrelationId);
            }
            await _bus.Publish(new ResumeActionGroup(actionGroup.CorrelationId, initiatingUser));
        }
        await context.RespondAsync(new Success());
    }

    /// <summary>
    /// Handles the <see cref="SetUserMode"/> request.
    /// </summary>
    public async Task Consume(ConsumeContext<SetUserMode> context)
    {
        (var agvCorrelationId, string? initiatingUser, var assignedActionPolicy, var currentActionPolicy, bool cancelManualActions) = context.Message;
        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);
        await _bus.Publish(new SetUserMode(agv.CorrelationId, initiatingUser, assignedActionPolicy, currentActionPolicy, cancelManualActions));
        await context.RespondAsync(new Success());
    }
}