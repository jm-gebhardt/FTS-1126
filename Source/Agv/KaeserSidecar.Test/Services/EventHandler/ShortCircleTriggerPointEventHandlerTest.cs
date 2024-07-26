using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class ShortCircleTriggerPointEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedShortCircleTriggerPoint_DropAt1050DroppingNextPickAvailable_ReportsToConveyor()
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));

        var fixture = new ShortCircleTriggerPointEventHandlerTestFixture()
            .WithHostOrder(nextPickOrder);

        var agvPassedShortCircleTriggerPoint = new AgvPassedShortCircleTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortCircleTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortCircleTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", true, "1020", Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortCircleTriggerPoint_DropAt1050DroppingNoNextPickAvailable_ReportsToConveyor()
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;

        var fixture = new ShortCircleTriggerPointEventHandlerTestFixture()
            .WithHostOrder(null);

        var agvPassedShortCircleTriggerPoint = new AgvPassedShortCircleTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortCircleTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortCircleTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", true, null, Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortCircleTriggerPoint_DropAt1050Picking_DoesNotReportToConveyor()
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Pick).State = ActionState.Started;

        var fixture = new ShortCircleTriggerPointEventHandlerTestFixture();

        var agvPassedShortCircleTriggerPoint = new AgvPassedShortCircleTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortCircleTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortCircleTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", true, null, Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortCircleTriggerPoint_PickAt1020Picking_ReportsToConveyor()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var actionGroup = order.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, orderDetails);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Pick).State = ActionState.Started;
       
        var fixture = new ShortCircleTriggerPointEventHandlerTestFixture()
            .WithHostOrder(order);

        var agvPassedShortCircleTriggerPoint = new AgvPassedShortCircleTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortCircleTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortCircleTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", false, "1020", Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortCircleTriggerPoint_PickAt1020Dropping_DoesNotReportToConveyor()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var actionGroup = order.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, orderDetails);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;

        var fixture = new ShortCircleTriggerPointEventHandlerTestFixture();

        var agvPassedShortCircleTriggerPoint = new AgvPassedShortCircleTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortCircleTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortCircleTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", false, "1020", Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortCircleTriggerPoint_NotPickDropActionGroup_DoesNotReportToConveyor()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var fixture = new ShortCircleTriggerPointEventHandlerTestFixture();

        var agvPassedShortCircleTriggerPoint = new AgvPassedShortCircleTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortCircleTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortCircleTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", false, "1020", Times.Never());
    }
}