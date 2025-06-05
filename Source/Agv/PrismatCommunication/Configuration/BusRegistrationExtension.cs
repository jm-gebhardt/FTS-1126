using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.Graphs.Requests;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.SystemStatus;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.Station;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.SystemStatus;
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
            partitioner.PartitionMessageById<AbortHostOrder, HostOrderCommandHandler>(c);
            c.Message<PassTriggerPoint>(x => x.UsePartitioner(partitioner, p => p.Message.DroppingHostOrderCorrelationId));

            var huPartitioner = c.CreatePartitioner(16);
            c.Message<CreateHostOrder>(x => x.UsePartitioner(huPartitioner, p => p.Message.Hu));
            c.Message<CancelHostOrder>(x => x.UsePartitioner(huPartitioner, p => p.Message.Hu));
        });

        cfg.AddConsumer<SystemStatusCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(16);
            c.Message<GetSystemStatus>(x => x.UsePartitioner(partitioner, _ => SystemStatus.GetId()));
            c.Message<SetReady>(x => x.UsePartitioner(partitioner, _ => SystemStatus.GetId()));
            c.Message<SetNotReady>(x => x.UsePartitioner(partitioner, _ => SystemStatus.GetId()));
        });
        cfg.AddConsumer<AgvEventHandler>();
        cfg.AddConsumer<ActionGroupEventHandler>();
        cfg.AddConsumer<TriggerEventHandler>();
        cfg.AddConsumer<QueueEventHandler>();
        cfg.AddConsumer<StationEventHandler>();
        cfg.AddConsumer<StationCommandHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(16);
            partitioner.PartitionMessageById<RemoveAgvFromStation, StationCommandHandler>(c);
            partitioner.PartitionMessageById<AddAgvToStation, StationCommandHandler>(c);
        });

        cfg.AddRequestClient<AddOrUpdateStationGroup>();
        cfg.AddRequestClient<GetQueueFillQuantities>();
        cfg.AddRequestClient<GetTriggerMapping>();
        return cfg;
    }
}