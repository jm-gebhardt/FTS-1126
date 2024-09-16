using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TriggerService.Services;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.TriggerService.Configuration;

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
        cfg.AddConsumer<TriggerEventHandler>(c =>
        {
            var partitioner = c.CreatePartitioner(16);
            partitioner.PartitionMessageById<DefaultTriggerConfigurationUpdated, TriggerEventHandler>(c);
        });
        return cfg;
    }
}