using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Gebhardt.Acc.Statistics.Services.CommandHandler;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.Statistics.Configuration;

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
        cfg.AddConsumer<GridEventHandler>().Endpoint(c => c.InstanceId = nameof(GridEventHandler));
        cfg.AddConsumer<HousekeepingCommandHandler>().Endpoint(c => c.InstanceId = nameof(HousekeepingCommandHandler));
        cfg.AddConsumer<PositionEventHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<AgvPositionReceived>(x => x.UsePartitioner(partitioner, p => p.Message.SerialNumber));
        });
        return cfg;
    }
}