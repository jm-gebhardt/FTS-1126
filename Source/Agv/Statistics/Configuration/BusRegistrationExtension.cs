using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Messages.Contracts;
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
    private static void AddGridPositionAttachedHandler<THandler, TMessage>(IBusRegistrationConfigurator cfg)
        where THandler : class, IConsumer
        where TMessage : class, IAgvSerialNumber, IWithAgvPosition, IMessageCreatedDate
    {
        cfg.AddConsumer<THandler>();
        cfg.AddConsumer<GridPositionPersistedForwarder<TMessage>>();
    }

    /// <summary>
    /// Add bus consumers.
    /// </summary>
    [PublicAPI]
    public static IBusRegistrationConfigurator AddBusConsumers(this IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<GridEventHandler>(c => { c.UseConcurrentMessageLimit(1); });
        cfg.AddConsumer<HousekeepingCommandHandler>(c => { c.UseConcurrentMessageLimit(1); });

        cfg.AddConsumer<AgvPositionReceivedHandler>();
        AddGridPositionAttachedHandler<AgvErrorEventHandler, AgvErrorReceived>(cfg);
        AddGridPositionAttachedHandler<WifiMetricsEventHandler, AgvWifiMetricsReceived>(cfg);

        cfg.AddConsumer<ErrorCommandHandler>();
        cfg.AddConsumer<WifiMetricsCommandHandler>();
        return cfg;
    }
}