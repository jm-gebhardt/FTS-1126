using Gebhardt.StoreWare.Agv.Common.Testing.Fixtures;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Test.Services.BackgroundServices;

/// <summary>
/// Provides a test fixture for the <see cref="OfflineWatchdogInitializer"/>.
/// </summary>
public class OfflineWatchdogInitializerTestFixture : BackgroundServiceTestFixture<OfflineWatchdogInitializer>
{
    private readonly Mock<IAgvDb> _agvDbMock = new();

    /// <inheritdoc cref="OfflineWatchdogInitializerTestFixture"/>>
    public OfflineWatchdogInitializerTestFixture()
    {
        AddServices(c => c.AddTransient(_ => _agvDbMock.Object));
        Build();
    }

    public OfflineWatchdogInitializerTestFixture AddAgvs(ICollection<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvDbMock.Setup(x => x.Agvs.GetAllAgvsAsync()).ReturnsAsync(agvs);
        return this;
    }
}