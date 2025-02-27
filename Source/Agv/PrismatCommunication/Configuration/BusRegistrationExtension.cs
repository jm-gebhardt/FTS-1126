using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;
using Gebhardt.Acc.PrismatCommunication.Services.EventHandler;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.PrismatCommunication.Configuration;

/// <summary>
/// Provides extension methods for the bus registration.
/// </summary>
public static class BusRegistrationExtension
{
    /// <summary>
    /// Add bus consumers.
    /// </summary>
    [PublicAPI]
    public static IBusRegistrationConfigurator AddBusConsumers(this IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<HostOrderCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(16);
            partitioner.PartitionMessageById<FinishHostOrder, HostOrderCommandHandler>(c);
            partitioner.PartitionMessageById<FinishPick, HostOrderCommandHandler>(c);
            partitioner.PartitionMessageById<CancelInvalidHostOrder, HostOrderCommandHandler>(c);
            c.Message<PassTriggerPoint>(x => x.UsePartitioner(partitioner, p => p.Message.DroppingHostOrderCorrelationId));

            var huPartitioner = c.CreatePartitioner(16);
            c.Message<CreateHostOrder>(x => x.UsePartitioner(huPartitioner, p => p.Message.Hu));
            c.Message<CancelHostOrder>(x => x.UsePartitioner(huPartitioner, p => p.Message.Hu));
        });
        cfg.AddConsumer<ActionGroupEventHandler>();
        cfg.AddConsumer<TriggerEventHandler>();
        cfg.AddConsumer<QueueEventHandler>();
        cfg.AddConsumer<StationEventHandler>();
        return cfg;
    }
}