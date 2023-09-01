using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using TransportOrder.Messages.Commands.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services.CommandHandler.Agv;

[TestClass]
public class AgvCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_ValidData_SetsAgvIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddChargingAgv(new List<IAgv>{ agv });
        fixture.SetupAgv(hasPendingActions:true);

        await fixture.TestHarness!.Bus.Publish(new TryReactivateChargingAgv());
        (await fixture.ConsumerHarness!.Consumed.Any<TryReactivateChargingAgv>()).ShouldBeTrue();
        fixture.AgvSagaCommunicationService.Verify(r =>
            r.SetIdleAsync(agv.CorrelationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_NoPendingAction_AgvNotSetIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        var agv = AgvGenerator.GenerateIdleAgv();
        fixture.AddChargingAgv(new List<IAgv>{ agv });
        fixture.SetupAgv(hasPendingActions: false);

        await fixture.TestHarness!.Bus.Publish(new TryReactivateChargingAgv());
        (await fixture.ConsumerHarness!.Consumed.Any<TryReactivateChargingAgv>()).ShouldBeTrue();
        fixture.AgvSagaCommunicationService.Verify(r =>
            r.SetIdleAsync(agv.CorrelationId, It.IsAny<CancellationToken>()), Times.Never());
    }

    [TestMethod]
    public async Task Consume_TryReactivateChargingAgv_NoChargingAgvs_AgvNotSetIdle()
    {
        var fixture = new AgvCommandHandlerTestFixture();
        fixture.AddChargingAgv(new List<IAgv>());
        await fixture.TestHarness!.Bus.Publish(new TryReactivateChargingAgv());
        (await fixture.ConsumerHarness!.Consumed.Any<TryReactivateChargingAgv>()).ShouldBeTrue();
        fixture.AgvSagaCommunicationService.Verify(r =>
            r.SetIdleAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
    }
}