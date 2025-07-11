using Gebhardt.Acc.Common.Messages.Graphs.Events;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Core.Services.EventHandler;

/// <summary>
/// provides handlers for station-related events.
/// </summary>
[UsedImplicitly]
public class StationEventHandler(IAgvDb agvDb):
    IConsumer<StationUnlocked>
{
    /// <summary>
    /// Handles the <see cref="StationUnlocked"/> evnet.
    /// </summary>
    public async Task Consume(ConsumeContext<StationUnlocked> context)
    {
        await context.Publish(new TryReactivateParkingAgvs());

        // With infinite charging there could be no parking agvs,
        // reactivate also charging agvs
        ICollection<Agv> agvs = await agvDb.Agvs.GetChargingAgvsWithAtLeastMinChargeUpLevel();
        if (agvs.Count == 0)
        {
            return;
        }
        await context.Publish(new TryReactivateChargingAgv(agvs.First().CorrelationId));
    }
}