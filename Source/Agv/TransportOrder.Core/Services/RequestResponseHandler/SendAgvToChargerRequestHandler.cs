using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Map;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.RequestResponseHandler;

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
    private readonly IBus _bus;

    /// <inheritdoc cref="SendAgvToChargerRequestHandler"/>
    public SendAgvToChargerRequestHandler(IAgvDb agvDb,
        IActionGroupRepository actionGroupDb,
        IChargingPointManager chargingPointManager,
        INodeStateRepository nodeStateRepository,
        IBus bus
    )
    {
        _agvDb = agvDb;
        _actionGroupDb = actionGroupDb;
        _chargingPointManager = chargingPointManager;
        _nodeStateRepository = nodeStateRepository;
        _bus = bus;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SendAgvToCharger> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        var currentAction = await _actionGroupDb.GetCurrentAgvActionAsync(agv.CorrelationId);

        var charger = await _chargingPointManager.ReserveChargerAsync(agv, startNodeId: currentAction?.TargetNodeId);
        await PublishNodeStateChange(charger.Id);

        await context.Publish(new StartChargingActionGroup(Guid.NewGuid(), agvCorrelationId, charger.Id, ActionGroupCreator.TransportOrderWebApi));

        await context.RespondAsync(new Success());
    }

    private async Task PublishNodeStateChange(string nodeId)
    {
        var nodeState = await _nodeStateRepository.GetNodeStateAsync(nodeId);
        await _bus.Publish(new NodeStateChanged(nodeState!));
    }
}