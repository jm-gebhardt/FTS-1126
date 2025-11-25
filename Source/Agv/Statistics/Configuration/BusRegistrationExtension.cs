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
        cfg.AddConsumer<GridEventHandler>(c => { c.UseConcurrentMessageLimit(1); });
        cfg.AddConsumer<HousekeepingCommandHandler>(c => { c.UseConcurrentMessageLimit(1); });

        cfg.AddConsumer<AgvPositionReceivedHandler>();
        cfg.AddConsumer<AgvErrorEventHandler>();
        cfg.AddConsumer<WifiMetricsEventHandler>();

        cfg.AddConsumer<ErrorCommandHandler>();
        cfg.AddConsumer<GridCellStayCommandHandler>();
        cfg.AddConsumer<WifiMetricsCommandHandler>();
        return cfg;
    }
}