using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Commands.Agv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.BackgroundServices;

[TestClass]
public class OfflineWatchdogInitializerTest
{
    [TestMethod]
    public async Task ExecuteAsync_ValidData_PublishesResetOfflineWatchdog()
    {
        var agv = AgvGenerator.GenerateIdleAgv();
        var fixture = new OfflineWatchdogInitializerTestFixture()
            .AddAgvs([agv]);

        await fixture.HostedService!.StartAsync(CancellationToken.None);

        (await fixture.TestHarness.Published.Any<ResetOfflineWatchdog>()).ShouldBeTrue();
    }

    [TestMethod]
    public async Task ExecuteAsync_NoAgvs_DoesNotPublishResetOfflineWatchdog()
    {
        var fixture = new OfflineWatchdogInitializerTestFixture()
            .AddAgvs([]);

        await fixture.HostedService!.StartAsync(CancellationToken.None);

        (await fixture.TestHarness.Published.Any<ResetOfflineWatchdog>()).ShouldBeFalse();
    }
}