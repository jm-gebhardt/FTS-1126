using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;

/// <summary>
/// Testing fixture to test background service
/// </summary>
/// <typeparam name="TBackgroundService"></typeparam>
[PublicAPI]
public abstract class BackgroundServiceTestFixture<TBackgroundService> : MassTransitTestFixture where TBackgroundService : BackgroundService
{
    /// <summary>
    /// Started background service
    /// </summary>
    public TBackgroundService HostedService { get; private set; } = null!;

    /// <summary>
    /// Test Fixture to be used for background service tests
    /// </summary>
    protected BackgroundServiceTestFixture()
    {
        AddServices(s => s.AddHostedService<TBackgroundService>());
    }

    protected new void Build()
    {
        base.Build();
        HostedService = (Provider.GetServices<IHostedService>().First(x => x is TBackgroundService) as TBackgroundService)!;
    }
}