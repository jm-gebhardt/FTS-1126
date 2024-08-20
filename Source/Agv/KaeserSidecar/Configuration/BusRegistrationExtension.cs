
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
    /// <summary>
    /// Add bus consumers.
    /// </summary>
    [PublicAPI]
    public static IBusRegistrationConfigurator AddBusConsumers(this IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<KaeserGraphEventHandler>();
        cfg.AddConsumer<AgvActionCommandHandler>();
        cfg.AddConsumer<DecisionPointEventHandler>(); // Registered as singleton externally
        cfg.AddConsumer<CargoTriggerPointEventHandler, SingleActiveHandlerDefinition<CargoTriggerPointEventHandler>>(c => c.ConcurrentMessageLimit = 1);
        cfg.AddConsumer<ShortLoopTriggerPointEventHandler, SingleActiveHandlerDefinition<ShortLoopTriggerPointEventHandler>>(c => c.ConcurrentMessageLimit=1);
        return cfg;
    }
}