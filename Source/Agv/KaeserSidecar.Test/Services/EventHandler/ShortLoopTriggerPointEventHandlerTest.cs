using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class ShortLoopTriggerPointEventHandlerTest
{
    [TestMethod]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_DropAt1050DroppingNextPickAvailable_ReportsToConveyor()
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));

        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithHostOrder(nextPickOrder);

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", true, "1020", Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_DropAt1050DroppingNoNextPickAvailable_ReportsToConveyor()
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;

        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithHostOrder(null);

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", true, null, Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_DropAt1050Picking_DoesNotReportToConveyor()
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Pick).State = ActionState.Started;

        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture();

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", true, null, Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_PickAt1020Picking_ReportsToConveyor()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var actionGroup = order.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, orderDetails);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Pick).State = ActionState.Started;
       
        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithHostOrder(order);

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", false, "1020", Times.Once());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_PickAt1020Dropping_DoesNotReportToConveyor()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var actionGroup = order.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, orderDetails);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;

        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture();

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", false, "1020", Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_NotPickDropActionGroup_DoesNotReportToConveyor()
    {
        var actionGroup = AgvActionGenerator.GenerateChargeActionGroup();
        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture();

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", false, "1020", Times.Never());
    }
}