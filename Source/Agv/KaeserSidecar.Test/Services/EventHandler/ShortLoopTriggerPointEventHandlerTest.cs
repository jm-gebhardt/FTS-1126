using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class ShortLoopTriggerPointEventHandlerTest
{
    [DataTestMethod]
    [DataRow(false, "1020")]
    [DataRow(true, null)]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_DropAt1050DroppingNextPickAvailable_ReportsToConveyor(bool hasLowBatteryLevel, string? pickStation)
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));

        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithHostOrder(dropHostOrder)
            .WithPickHostOrder(nextPickOrder)
            .WithAgvHasLowBatteryLevel(hasLowBatteryLevel);

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();
    
        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedShortLoopTriggerPoint>();
        (await fixture.GetFirstPublishedMessageAsync<DroppingAgvPassedShortLoopTriggerPoint>()).ShouldNotPickNext.ShouldBe(hasLowBatteryLevel || pickStation == null);

        fixture.VerifyReportingToConveyors("WE03", true, pickStation, Times.Once());
        fixture.OrderRepository.HasOrderBeenReported(pickStation??"", nextPickOrder.CorrelationId).ShouldBe(pickStation!=null);
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_DropAt1050DroppingNoNextPickAvailable_ReportsToConveyor()
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;

        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithPickHostOrder(null)
            .WithHostOrder(dropHostOrder)
            .WithAgvHasLowBatteryLevel(false);

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<DroppingAgvPassedShortLoopTriggerPoint>();
        (await fixture.GetFirstPublishedMessageAsync<DroppingAgvPassedShortLoopTriggerPoint>()).ShouldNotPickNext.ShouldBeTrue();
        
        fixture.VerifyReportingToConveyors("WE03", true, null, Times.Once());
        fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Source, dropHostOrder.CorrelationId).ShouldBeFalse();
        fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Destination, dropHostOrder.CorrelationId).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_DropAt1050Picking_DoesNotReportToConveyor()
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Pick).State = ActionState.Started;

        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithHostOrder(dropHostOrder)
            .WithPickHostOrder(null);

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedShortLoopTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", true, null, Times.Never());
        fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Source, dropHostOrder.CorrelationId).ShouldBeFalse();
        fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Destination, dropHostOrder.CorrelationId).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_PickAt1020Picking_ReportsToConveyor()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var actionGroup = order.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, orderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Pick).State = ActionState.Started;
       
        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithHostOrder(order)
            .WithPickHostOrder(null);

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedShortLoopTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", false, "1020", Times.Once());
        fixture.OrderRepository.HasOrderBeenReported(orderDetails.Source, order.CorrelationId).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvPassedShortLoopTriggerPoint_PickAt1020Dropping_DoesNotReportToConveyor()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var actionGroup = order.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, orderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;

        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithHostOrder(order)
            .WithPickHostOrder(null);

        var agvPassedShortLoopTriggerPoint = new AgvPassedShortLoopTriggerPoint(AgvDataGenerator.GenerateAgvData(Guid.NewGuid()),
            new AgvStatus(), actionGroup, [], []);

        await fixture.TestHarness.Bus.Publish(agvPassedShortLoopTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<AgvPassedShortLoopTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedShortLoopTriggerPoint>();

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
        await fixture.ShouldNotHavePublishedAsync<DroppingAgvPassedShortLoopTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", false, "1020", Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvStartedShortLoopPick_OrderHasNotBeenReported_ReportOrder()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithHostOrder(order);
        fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId).ShouldBeFalse();

        await fixture.TestHarness.Bus.Publish(new AgvStartedShortLoopPick("WE03", order.CorrelationId, "1020"));
        await fixture.ShouldHaveConsumedAsync<AgvStartedShortLoopPick>();

        fixture.VerifyReportingToConveyors("WE03", false, "1020", Times.Once());
        fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvStartedShortLoopPick_OrderHasAlreadyBeenReported_DoesNotReportOrder()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithHostOrder(order);
        fixture.OrderRepository.StorePickOrderReport("1020", order.CorrelationId);
        fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId).ShouldBeTrue();

        await fixture.TestHarness.Bus.Publish(new AgvStartedShortLoopPick("WE03", order.CorrelationId, "1020"));
        await fixture.ShouldHaveConsumedAsync<AgvStartedShortLoopPick>();

        fixture.VerifyReportingToConveyors("WE03", false, "1020", Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvFinishedShortLoopPick_OrderHasBeenReported_RemovesReport()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new ShortLoopTriggerPointEventHandlerTestFixture()
            .WithHostOrder(order);
        fixture.OrderRepository.StorePickOrderReport("1020", order.CorrelationId);
        fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId).ShouldBeTrue();

        await fixture.TestHarness.Bus.Publish(new AgvFinishedShortLoopPick(order.CorrelationId, "1020"));
        await fixture.ShouldHaveConsumedAsync<AgvFinishedShortLoopPick>();
        fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId).ShouldBeFalse();
    }
}