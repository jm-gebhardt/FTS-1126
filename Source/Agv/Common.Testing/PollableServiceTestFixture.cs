using Gebhardt.StoreWare.Agv.Common.Services;
using JetBrains.Annotations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.Common.Testing;

/// <summary>
/// Testing fixture to test pollable service
/// </summary>
/// <typeparam name="TPollable"></typeparam>
[PublicAPI]
public class PollableServiceTestFixture<TPollable> where TPollable : class, IPollable
{
    /// <summary>
    /// Service provider
    /// </summary>
    public readonly ServiceProvider Provider;

    /// <summary>
    /// Started TestHarness
    /// </summary>
    public readonly ITestHarness TestHarness;

    /// <summary>
    /// Registered Pollable Service
    /// </summary>
    public readonly TPollable Pollable;

    /// <summary>
    /// Test Fixture to be used for background service tests
    /// </summary>
    /// <param name="services">Service Collection instance with MassTransitTestHarness added</param>
    /// <param name="busRegistrationConfigurator">MassTransit Registration with consumer already added</param>
    public PollableServiceTestFixture(Action<IServiceCollection>? services = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null)
    {
        var collection = new ServiceCollection()
            .AddMassTransitTestHarness(x =>
            {
                busRegistrationConfigurator?.Invoke(x);
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.ConfigureEndpoints(context);
                });
            })
            .AddSingleton<TPollable>();

        services?.Invoke(collection);

        Provider = collection.BuildServiceProvider(true);
        TestHarness = Provider.GetTestHarness();
        Pollable = Provider.GetService<TPollable>()!;

        TestHarness.Start();
    }
}