using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.OrderTranslator.Messages;
using Gebhardt.Acc.OrderTranslator.Services.CommandHandler;
using Gebhardt.Acc.OrderTranslator.Services.EventHandler;
using Gebhardt.Acc.OrderTranslator.Services.RequestResponseHandlers;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.OrderTranslator.Configuration;

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
        cfg.AddConsumer<CargoTriggerPointEventHandler, SingleActiveHandlerDefinition<CargoTriggerPointEventHandler>>(c => c.ConcurrentMessageLimit = 1);
        cfg.AddConsumer<ConveyorReportingEventHandler>(c =>
            {
                var partitioner = c.CreatePartitioner(PartitionCount);
                c.Message<DroppingAgvPassedDoppelspielTriggerPoint>(x =>
                    x.UsePartitioner(partitioner, p => p.Message.NextPickHostOrderCorrelationId.GetValueOrDefault()));
                c.Message<PickingAgvPassedDoppelspielTriggerPoint>(x =>
                    x.UsePartitioner(partitioner, p => p.Message.HostOrderCorrelationId));
            })
       ;
        cfg.AddConsumer<HostOrderCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<CreateActionGroup, HostOrderCommandHandler>(c);
        });
        cfg.AddConsumer<HostOrderEventHandler>();

        cfg.AddConsumer<StationCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            partitioner.PartitionMessageById<CreateOrUpdateStation, StationCommandHandler>(c);
        });
        cfg.AddConsumer<StationEventHandler>();
        cfg.AddConsumer<StationRequestHandler>();
        return cfg;
    }
}