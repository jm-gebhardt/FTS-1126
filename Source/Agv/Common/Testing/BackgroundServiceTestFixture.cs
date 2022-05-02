using JetBrains.Annotations;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.StoreWare.Agv.Common.Testing;

/// <summary>
/// Testing fixture to test background service
/// </summary>
/// <typeparam name="TBackgroundService"></typeparam>
[PublicAPI]
public class BackgroundServiceTestFixture<TBackgroundService> where TBackgroundService : BackgroundService
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
    /// Started background service
    /// </summary>
    public readonly TBackgroundService? HostedService;

    /// <summary>
    /// Consumer Test Fixture to be used for background service tests
    /// </summary>
    /// <param name="services">Service Collection instance with MassTransitTestHarness added</param>
    /// <param name="busRegistrationConfigurator">MassTransit Registration with consumer already added</param>
    public BackgroundServiceTestFixture(Action<IServiceCollection>? services = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null)
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
            .AddHostedService<TBackgroundService>();

        services?.Invoke(collection);

        Provider = collection.BuildServiceProvider(true);
        TestHarness = Provider.GetTestHarness();
        HostedService = Provider.GetServices<IHostedService>().FirstOrDefault(x => x is TBackgroundService) as TBackgroundService;

        TestHarness.Start();
    }
}