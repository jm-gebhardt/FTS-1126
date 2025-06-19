
using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.KaeserSidecar.Messages.JamState;
using Gebhardt.Acc.KaeserSidecar.Services.CommandHandler;
using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.KaeserSidecar.Configuration;

/// <summary>
/// Provides extension methods for the bus registration.
/// </summary>
public static class BusRegistrationExtension
{
    private const int PartitionCount = 16;

    /// <summary>
    /// Add bus consumers.
    /// </summary>
    [PublicAPI]
    public static IBusRegistrationConfigurator AddBusConsumers(this IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<KaeserGraphEventHandler>();
        cfg.AddConsumer<AgvActionCommandHandler>();
        cfg.AddConsumer<DecisionPointEventHandler>();
        cfg.AddConsumer<ShortLoopWaitingTriggerPointEventHandler>(); 
        cfg.AddConsumer<CargoTriggerPointEventHandler, SingleActiveHandlerDefinition<CargoTriggerPointEventHandler>>(c => c.ConcurrentMessageLimit = 1);
        cfg.AddConsumer<ConveyorReportingEventHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                c.Message<DroppingAgvPassedDoppelspielTriggerPoint>(x =>
                    x.UsePartitioner(partitioner, p => p.Message.NextPickHostOrderCorrelationId.GetValueOrDefault()));
                c.Message<PickingAgvPassedDoppelspielTriggerPoint>(x =>
                    x.UsePartitioner(partitioner, p => p.Message.HostOrderCorrelationId));
                c.Message<AgvStartedPick>(x =>
                    x.UsePartitioner(partitioner, p => p.Message.OrderId));
                c.Message<AgvFinishedPick>(x =>
                    x.UsePartitioner(partitioner, p => p.Message.OrderId));
            })
       ;
        cfg.AddConsumer<DoppelspielTriggerPointEventHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<AgvPassedDoppelspielTriggerPoint>(x => x.UsePartitioner(partitioner, p => p.Message.AgvData.CorrelationId));
        });

        cfg.AddConsumer<StationCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<OccupyStation>(x => x.UsePartitioner(partitioner, p => p.Message.Station));
            c.Message<FreeStation>(x => x.UsePartitioner(partitioner, p => p.Message.Station));
        });

        cfg.AddConsumer<JamStateEventHandler>();
        cfg.AddConsumer<JamStateCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<UpdateJamState>(x => x.UsePartitioner(partitioner, p => p.Message.CorrelationId));
        });
        cfg.AddRequestClient<GetJamState>();

        return cfg;
    }
}