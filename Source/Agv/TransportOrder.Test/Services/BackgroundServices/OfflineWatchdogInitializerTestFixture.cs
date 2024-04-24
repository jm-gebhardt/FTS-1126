using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.TransportOrder.Core.Services.BackgroundServices;
using Gebhardt.Acc.TransportOrder.Domain;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.TransportOrder.Test.Services.BackgroundServices;

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

    public OfflineWatchdogInitializerTestFixture AddAgvs(List<TransportOrder.Domain.Models.Agv.Agv> agvs)
    {
        _agvDbMock.AddAgvs(agvs);
        return this;
    }
}