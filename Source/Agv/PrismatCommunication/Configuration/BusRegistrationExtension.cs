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
            partitioner.PartitionMessageById<CreateHostOrder, HostOrderCommandHandler>(c);
            partitioner.PartitionMessageById<FinishHostOrder, HostOrderCommandHandler>(c);
            partitioner.PartitionMessageById<PassDecisionPoint, HostOrderCommandHandler>(c);
            partitioner.PartitionMessageById<QueuePickDropActionGroupForAgv, HostOrderCommandHandler>(c);
            partitioner.PartitionMessageById<QueueActionGroupWithUnknownDropTargetForAgv, HostOrderCommandHandler>(c);
        });
        cfg.AddConsumer<ActionGroupEventHandler>();
        cfg.AddConsumer<HostOrderEventHandler>();
        return cfg;
    }
}