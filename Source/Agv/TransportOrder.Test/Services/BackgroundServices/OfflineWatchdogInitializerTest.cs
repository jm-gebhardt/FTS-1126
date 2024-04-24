using Gebhardt.Acc.TransportOrder.Messages.Commands.Agv;
using Gebhardt.Acc.TransportOrder.Test.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.Acc.TransportOrder.Test.Services.BackgroundServices;

[TestClass]
public class OfflineWatchdogInitializerTest
{
    [TestMethod]
    public async Task ExecuteAsync_ValidData_PublishesResetOfflineWatchdog()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new OfflineWatchdogInitializerTestFixture()
            .AddAgvs([agv]);

        await fixture.HostedService.StartAsync(CancellationToken.None);

        await fixture.ShouldHavePublishedAsync<ResetOfflineWatchdog>();
    }

    [TestMethod]
    public async Task ExecuteAsync_NoAgvs_DoesNotPublishResetOfflineWatchdog()
    {
        var fixture = new OfflineWatchdogInitializerTestFixture()
            .AddAgvs([]);

        await fixture.HostedService.StartAsync(CancellationToken.None);

        await fixture.ShouldNotHavePublishedAsync<ResetOfflineWatchdog>();
    }
}