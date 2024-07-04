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
        return cfg;
    }
}