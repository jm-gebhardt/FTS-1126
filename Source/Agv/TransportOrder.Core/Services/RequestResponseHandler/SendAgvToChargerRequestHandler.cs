using Gebhardt.StoreWare.Agv.Common.Exceptions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.ActionGroup;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
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

    /// <inheritdoc cref="SendAgvToChargerRequestHandler"/>
    public SendAgvToChargerRequestHandler(IAgvDb agvDb, IActionGroupRepository actionGroupDb, IChargingPointManager chargingPointManager)
    {
        _agvDb = agvDb;
        _actionGroupDb = actionGroupDb;
        _chargingPointManager = chargingPointManager;
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<SendAgvToCharger> context)
    {
        var agvCorrelationId = context.Message.CorrelationId;

        var agv = await _agvDb.Agvs.GetAgvAsync(agvCorrelationId);

        var currentAction = await _actionGroupDb.GetCurrentAgvActionAsync(agv.CorrelationId);

        string? nodeId = currentAction?.TargetNodeId;

        GraphNode charger;
        try
        {
            charger = await _chargingPointManager.ReserveChargerAsync(agv, nodeId);
        }
        catch 
        {
            throw new NoChargeStationAvailableException();
        }

        await context.Publish(new StartChargingActionGroup(agvCorrelationId, charger.Id, ActionGroupCreator.TransportOrderWebApi));

        await context.RespondAsync(new Success());
    }
}