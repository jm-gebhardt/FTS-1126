using Gebhardt.Acc.KaeserSidecar.Services.EventHandler;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Consumer;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.Acc.KaeserSidecar.Configuration;

/// <summary>
/// Provides extension methods for singleton consumer registrations
/// </summary>
[PublicAPI]
public static class SingletonBusConsumerExtension
{
    /// <summary>
    /// Register bus consumers as singleton services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static IServiceCollection RegisterSingletonConsumers(this IServiceCollection services)
    {
        services.AddSingleton<DecisionPointEventHandler>();
        return services;
    }

    /// <summary>
    /// Register consumer factories of singleton consumers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns></returns>
    public static IServiceCollection RegisterSingletonConsumerFactories(this IServiceCollection services)
    {
        services.AddSingleton<IConsumerFactory<DecisionPointEventHandler>>(provider =>
            new DelegateConsumerFactory<DecisionPointEventHandler>(provider.GetRequiredService<DecisionPointEventHandler>));
        return services;
    }
}