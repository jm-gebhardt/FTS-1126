using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Domain.Services.LogEntry;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Gebhardt.Acc.TransportOrder.Core.Services.RequestResponseHandler;

/// <summary>
/// Defines a handler for SendAgvToCharger.
/// </summary>
[UsedImplicitly]
public class SendAgvToChargerRequestHandler : IConsumer<SendAgvToCharger>
{
    private readonly IAgvDb _agvDb;
    private readonly IActionGroupRepository _actionGroupDb;
    private readonly IChargingPointManager _chargingPointManager;
    private readonly INodeRepository _nodeRepository;
    private readonly IStationRepository _stationRepository;
    private readonly IBus _bus;
    private readonly ILogger<SendAgvToChargerRequestHandler> _logger;
    private readonly ILoggingService _loggingService;

    /// <inheritdoc cref="SendAgvToChargerRequestHandler"/>
    public SendAgvToChargerRequestHandler(
        ILogger<SendAgvToChargerRequestHandler> logger,
        IAgvDb agvDb,
        IActionGroupRepository actionGroupDb,
        IChargingPointManager chargingPointManager,
        IStationRepository stationRepository,
        INodeRepository nodeRepository,
        IBus bus,
        ILoggingService loggingService
    )
    {
        _agvDb = agvDb;
        _actionGroupDb = actionGroupDb;
        _chargingPointManager = chargingPointManager;
        _nodeRepository = nodeRepository;
        _logger = logger;
        _stationRepository = stationRepository;
        _bus = bus;
        _loggingService = loggingService;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SendAgvToCharger> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;
        string user = context.Message.User;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (!agv.CanStartManualOperationalAction)
        {
            throw new ActionNotAllowedInModeException(agv.CorrelationId, agv.Mode.Type.ToString());
        }

        var currentAction = await _actionGroupDb.GetCurrentAgvActionAsync(agv.CorrelationId);

        var charger = await _chargingPointManager.ReserveChargerAsync(agv, startNodeId: currentAction?.Target.Node?.Id, ignoreLocked: true);
        await PublishNodeStateChange(charger.Id);

        await context.Publish(new CreateChargingActionGroup(Guid.NewGuid(), agvCorrelationId, charger, ActionGroupCreator.TransportOrderWebApi));
        await _loggingService.LogAgv(_logger, LogLevel.Information, LogCodes.AgvOperatorInteraction, agvCorrelationId, null, "Operator interaction", "Operator interacted with agv", new Dictionary<string, object?> { { LogLabels.User, user }, { LogLabels.Operation, LogCodes.AgvManualCharge } });

        await context.RespondAsync(new Success());
    }

    private async Task PublishNodeStateChange(Guid nodeId)
    {
        var node= await _nodeRepository.GetNodeAsync(nodeId);
        var station = await _stationRepository.GetStation(nodeId);
        if (node.Lock == null)
        {
            return;
        }
        await _bus.Publish(new NodeStateChanged(node.Lock!, node.Name, station.Id, station.LockedBy, station.LockReason, station.LockDescription));
    }
}