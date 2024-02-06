using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.BackgroundServices;

/// <summary>
/// Provides a test fixture for the <see cref="OfflineWatchdogInitializer"/>.
/// </summary>
public class OfflineWatchdogInitializerTestFixture : BackgroundServiceTestFixture<OfflineWatchdogInitializer>
{
    private static readonly Mock<IAgvDb> _agvDbMock = new();

    /// <inheritdoc cref="OfflineWatchdogInitializerTestFixture"/>>
    public OfflineWatchdogInitializerTestFixture(Action<IServiceCollection>? services = null, Action<IBusRegistrationConfigurator>? busRegistrationConfigurator = null)
        : base(
            services: collection =>
            {
                services?.Invoke(collection);
                collection.AddTransient(_ => _agvDbMock.Object);
            },
            busRegistrationConfigurator: busRegistrationConfigurator)
    {
    }

    public OfflineWatchdogInitializerTestFixture AddAgvs(ICollection<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvDbMock.Setup(x => x.Agvs.GetAllAgvsAsync()).ReturnsAsync(agvs);
        return this;
    }
}