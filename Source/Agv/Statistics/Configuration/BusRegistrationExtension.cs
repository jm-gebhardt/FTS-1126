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
    /// <summary>
    /// Add bus consumers.
    /// </summary>
    [PublicAPI]
    public static IBusRegistrationConfigurator AddBusConsumers(this IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<GridEventHandler>(c =>
        {
            c.UseConcurrentMessageLimit(1);
        });
        cfg.AddConsumer<HousekeepingCommandHandler>(c =>
        {
            c.UseConcurrentMessageLimit(1);
        });
        cfg.AddConsumer<PositionAndErrorEventHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(1);
            c.Message<AgvPositionReceived>(x => x.UsePartitioner(partitioner, p => p.Message.SerialNumber));
            c.Message<AgvErrorReceived>(x => x.UsePartitioner(partitioner, p => p.Message.SerialNumber));
        });
        cfg.AddConsumer<ErrorCommandHandler>();
        return cfg;
    }
}