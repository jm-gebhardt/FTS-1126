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
    private readonly IServiceCollection _serviceCollection;

    /// <summary>
    /// Service provider
    /// </summary>
    public ServiceProvider Provider { get; private set; } = null!;

    /// <summary>
    /// Started TestHarness
    /// </summary>
    public ITestHarness TestHarness { get; private set; } = null!;

    /// <summary>
    /// Started background service
    /// </summary>
    public TBackgroundService? HostedService { get; private set; }

    /// <summary>
    /// Test Fixture to be used for background service tests
    /// </summary>
    public BackgroundServiceTestFixture()
    {
        _serviceCollection = new ServiceCollection()
            .AddMassTransitTestHarness(x => { x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); }); })
            .AddHostedService<TBackgroundService>();
    }

    protected void AddServices(Action<IServiceCollection> services)
    {
        services.Invoke(_serviceCollection);
    }

    protected void Build()
    {
        Provider = _serviceCollection.BuildServiceProvider(true);
        TestHarness = Provider.GetTestHarness();
        HostedService = Provider.GetServices<IHostedService>().FirstOrDefault(x => x is TBackgroundService) as TBackgroundService;

        TestHarness.Start();
    }
}