using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class CargoTriggerPointEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedCargoTriggerPoint_PickAt9060_ReportsToConveyor()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails( OrderGenerator.GenerateUnknownDropTargetOrder());
        var actionGroup = Helper.OrderGenerator.GenerateActionGroupFromOrder(hostOrder, ActionType.Pick);
        const bool hasCargo = false;
        var fixture = new CargoTriggerPointEventHandlerTestFixture()
            .WithHostOrder(hostOrder)
            .WithActionGroup(actionGroup);

        var agvPassedCargoTriggerPoint = new AgvPassedCargoTriggerPoint(hostOrder.AssignedAgv.GetValueOrDefault(), hostOrder.CorrelationId, hasCargo);

        await fixture.TestHarness.Bus.Publish(agvPassedCargoTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedCargoTriggerPoint>();

        fixture.VerifyReportingToConveyors(hasCargo, Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedCargoTriggerPoint_DropAt9070_ReportsToConveyor()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        var actionGroup = Helper.OrderGenerator.GenerateActionGroupFromOrder(hostOrder, ActionType.Drop);
        const bool hasCargo = true;
        var fixture = new CargoTriggerPointEventHandlerTestFixture()
            .WithHostOrder(hostOrder)
            .WithActionGroup(actionGroup);

        var agvPassedCargoTriggerPoint = new AgvPassedCargoTriggerPoint(hostOrder.AssignedAgv.GetValueOrDefault(), hostOrder.CorrelationId, hasCargo);

        await fixture.TestHarness.Bus.Publish(agvPassedCargoTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedCargoTriggerPoint>();

        fixture.VerifyReportingToConveyors(hasCargo, Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedCargoTriggerPoint_NotOrderTargeting9060Or9070_DoesNotReportToConveyor()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        var actionGroup = Helper.OrderGenerator.GenerateActionGroupFromOrder(hostOrder, ActionType.Pick);
        var fixture = new CargoTriggerPointEventHandlerTestFixture()
            .WithHostOrder(hostOrder)
            .WithActionGroup(actionGroup);

        var agvPassedCargoTriggerPoint = new AgvPassedCargoTriggerPoint(hostOrder.AssignedAgv.GetValueOrDefault(), hostOrder.CorrelationId, false);

        await fixture.TestHarness.Bus.Publish(agvPassedCargoTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedCargoTriggerPoint>();

        fixture.VerifyReportingToConveyors(false, Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvPassedCargoTriggerPoint_NoOrderInMessage_DoesNotReportToConveyor()
    {
        var fixture = new CargoTriggerPointEventHandlerTestFixture();

        var agvPassedCargoTriggerPoint = new AgvPassedCargoTriggerPoint(Guid.NewGuid(), null, false);

        await fixture.TestHarness.Bus.Publish(agvPassedCargoTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedCargoTriggerPoint>();

        fixture.VerifyReportingToConveyors(false, Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvPassedCargoTriggerPoint_OrderInMessageIsNotHostOrder_DoesNotReportToConveyor()
    {
        var fixture = new CargoTriggerPointEventHandlerTestFixture()
            .WithHostOrder(null);

        var agvPassedCargoTriggerPoint = new AgvPassedCargoTriggerPoint(Guid.NewGuid(), Guid.NewGuid(), false);

        await fixture.TestHarness.Bus.Publish(agvPassedCargoTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedCargoTriggerPoint>();

        fixture.VerifyReportingToConveyors(false, Times.Never());
    }
}