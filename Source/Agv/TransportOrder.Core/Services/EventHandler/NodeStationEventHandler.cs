using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.Common.Messages.Stations.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Node;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// provides handlers for node and station related events.
/// </summary>
[UsedImplicitly]
public class NodeStationEventHandler(IAgvDb agvDb) :
    IConsumer<StationUnlocked>,
    IConsumer<JamStateUpdated>,
    IConsumer<StationConfigured>
{
    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<StationUnlocked> context)
    {
        await context.Publish(new TryReactivateParkingAgvs());

        // With infinite charging there could be no parking agvs,
        // reactivate also charging agvs
        ICollection<Agv> agvs = await agvDb.Agvs.GetAgvsOnChargerWithAtLeastMinChargeUpLevel();
        if (agvs.Count == 0)
        {
            return;
        }
        IOrderedEnumerable<Agv> agvsOrderedByBatteryCharge = agvs.OrderByDescending(a => a.AgvStatus!.BatteryCharge);
        await context.Publish(new TryReactivateChargingAgv(agvsOrderedByBatteryCharge.First().CorrelationId));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<JamStateUpdated> context)
    {
        await context.Publish(new UpdateStationJamState(context.Message.StationId, context.Message.HasPalletJam));
    }

    /// <inheritdoc/>
    public async Task Consume(ConsumeContext<StationConfigured> context)
    {
        await context.Publish(new UpdateStationAdditionalInfo(context.Message.Id, context.Message.StationProperties));
    }
}