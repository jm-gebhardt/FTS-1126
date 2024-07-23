using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Statistics.Domain.Exceptions;
using Gebhardt.Acc.Statistics.Services.EventHandler;
using Gebhardt.Acc.Statistics.Services.CommandHandler;
using JetBrains.Annotations;
using MassTransit;
using ErrorEventHandler = Gebhardt.Acc.Statistics.Services.EventHandler.ErrorEventHandler;

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
        cfg.AddConsumer<GridEventHandler>(c =>
        {
            c.UseConcurrentMessageLimit(1);
        });
        cfg.AddConsumer<HousekeepingCommandHandler>(c =>
        {
            c.UseConcurrentMessageLimit(1);
        });
        cfg.AddConsumer<PositionEventHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(PartitionCount);
            c.Message<AgvPositionReceived>(x => x.UsePartitioner(partitioner, p => p.Message.SerialNumber));
        });
        cfg.AddConsumer<ErrorEventHandler>(c =>
            c.Message<AgvErrorReceived>(o => o.UseMessageRetry(r =>
            {
                // Corresponding GridAgvPositions may not have been saved yet (e.g. due to filtering). Retry in that case. 
                r.Interval(6, TimeSpan.FromSeconds(15));
                r.Handle<GridAgvPositionNotFoundException>();
            })));
        cfg.AddConsumer<ErrorCommandHandler>();
        return cfg;
    }
}