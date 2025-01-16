using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.KaeserSidecar.Messages;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.KaeserSidecar.Test.Services.EventHandler;

[TestClass]
public class ConveyorReportingEventHandlerTest
{
    [DataTestMethod]
    [DataRow(false, "1020")]
    [DataRow(true, null)]
    public async Task Consume_DroppingAgvPassedDoppelspielTriggerPoint_DropAt1050DroppingNextPickAvailable_ReportsToConveyor(bool hasLowBatteryLevel, string? pickStation)
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));

        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(dropHostOrder, nextPickOrder)
            .WithAgvHasLowBatteryLevel(hasLowBatteryLevel);

        var droppingAgvPassedDoppelspielTriggerPoint = new DroppingAgvPassedDoppelspielTriggerPoint(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId!,
            Guid.NewGuid(), dropHostOrder.CorrelationId, nextPickOrder.CorrelationId, !hasLowBatteryLevel);

        await fixture.TestHarness.Bus.Publish(droppingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();

        fixture.VerifyReportingToConveyors("WE03", dropHostOrder.Actions.First(a => a.ActionType == ActionType.Drop).Node, pickStation, Times.Once());
        fixture.OrderRepository.HasOrderBeenReported(pickStation??"", nextPickOrder.CorrelationId).ShouldBe(pickStation!=null);
    }

    [TestMethod]
    public async Task Consume_DroppingAgvPassedDoppelspielTriggerPoint_DropAt1050DroppingNoNextPickAvailable_ReportsToConveyor()
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;

        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(dropHostOrder)
            .WithAgvHasLowBatteryLevel(false);

        var droppingAgvPassedDoppelspielTriggerPoint = new DroppingAgvPassedDoppelspielTriggerPoint(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId!,
            Guid.NewGuid(), dropHostOrder.CorrelationId, null, false);

        await fixture.TestHarness.Bus.Publish(droppingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        
        fixture.VerifyReportingToConveyors("WE03", dropHostOrder.Actions.First(a => a.ActionType == ActionType.Drop).Node, null, Times.Once());
        fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Source, dropHostOrder.CorrelationId).ShouldBeFalse();
        fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Destination, dropHostOrder.CorrelationId).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AgvStartedPick_OrderHasNotBeenReported_ReportOrder()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order);
        fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId).ShouldBeFalse();

        await fixture.TestHarness.Bus.Publish(new AgvStartedPick("WE03", order.CorrelationId, "1020"));
        await fixture.ShouldHaveConsumedAsync<AgvStartedPick>();

        fixture.VerifyReportingToConveyors("WE03", null, "1020", Times.Once());
        fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvStartedPick_OrderHasAlreadyBeenReported_DoesNotReportOrder()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order);
        fixture.OrderRepository.StorePickOrderReport("1020", order.CorrelationId);
        fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId).ShouldBeTrue();

        await fixture.TestHarness.Bus.Publish(new AgvStartedPick("WE03", order.CorrelationId, "1020"));
        await fixture.ShouldHaveConsumedAsync<AgvStartedPick>();

        fixture.VerifyReportingToConveyors("WE03", null, "1020", Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvFinishedPick_OrderHasBeenReported_RemovesReport()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order);
        fixture.OrderRepository.StorePickOrderReport("1020", order.CorrelationId);
        fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId).ShouldBeTrue();

        await fixture.TestHarness.Bus.Publish(new AgvFinishedPick(order.CorrelationId, "1020"));
        await fixture.ShouldHaveConsumedAsync<AgvFinishedPick>();
        fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId).ShouldBeFalse();
    }
}