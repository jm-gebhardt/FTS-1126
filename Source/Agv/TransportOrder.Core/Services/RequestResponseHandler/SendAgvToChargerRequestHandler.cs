using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;

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
    private readonly INodeStateRepository _nodeStateRepository;
    private readonly IStationRepository _stationRepository;
    private readonly IBus _bus;

    /// <inheritdoc cref="SendAgvToChargerRequestHandler"/>
    public SendAgvToChargerRequestHandler(IAgvDb agvDb,
        IActionGroupRepository actionGroupDb,
        IChargingPointManager chargingPointManager,
        IStationRepository stationRepository,
        INodeStateRepository nodeStateRepository,
        IBus bus
    )
    {
        _agvDb = agvDb;
        _actionGroupDb = actionGroupDb;
        _chargingPointManager = chargingPointManager;
        _nodeStateRepository = nodeStateRepository;
        _stationRepository = stationRepository;
        _bus = bus;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SendAgvToCharger> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        if (!agv.CanStartManualOperationalAction)
        {
            throw new ActionNotAllowedInModeException(agv.CorrelationId, agv.Mode.Type.ToString());
        }

        var currentAction = await _actionGroupDb.GetCurrentAgvActionAsync(agv.CorrelationId);

        var charger = await _chargingPointManager.ReserveChargerAsync(agv, startNodeId: currentAction?.Target.NodeId);
        await PublishNodeStateChange(charger.Id);

        await context.Publish(new CreateChargingActionGroup(Guid.NewGuid(), agvCorrelationId, charger.Id, ActionGroupCreator.TransportOrderWebApi));

        await context.RespondAsync(new Success());
    }

    private async Task PublishNodeStateChange(string nodeId)
    {
        var nodeState = await _nodeStateRepository.GetNodeStateAsync(nodeId);
        var station = await _stationRepository.GetStation(nodeId);
        await _bus.Publish(new NodeStateChanged(nodeState!, station.Id, station.LockedBy, station.LockReason, station.LockDescription));
    }
}