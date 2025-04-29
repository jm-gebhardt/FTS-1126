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
            Guid.NewGuid(), dropHostOrder.CorrelationId, nextPickOrder.CorrelationId, !hasLowBatteryLevel, null);

        await fixture.TestHarness.Bus.Publish(droppingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();

        fixture.VerifyReportingToConveyors("PS03", dropHostOrder.Actions.First(a => a.ActionType == ActionType.Drop).Node, pickStation, dropHostOrder.Hu, Times.Once());
        (await fixture.OrderRepository.HasOrderBeenReported(pickStation??"", nextPickOrder.CorrelationId)).ShouldBe(pickStation!=null);
        (await fixture.OrderRepository.HasOrderBeenReported(dropHostOrder.Actions.First(a => a.ActionType == ActionType.Drop).Node, dropHostOrder.CorrelationId)).ShouldBeTrue();
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
            Guid.NewGuid(), dropHostOrder.CorrelationId, null, false, null);

        await fixture.TestHarness.Bus.Publish(droppingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        
        fixture.VerifyReportingToConveyors("PS03", dropHostOrder.Actions.First(a => a.ActionType == ActionType.Drop).Node, null, dropHostOrder.Hu, Times.Once());
        (await fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Source, dropHostOrder.CorrelationId)).ShouldBeFalse();
        (await fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Destination, dropHostOrder.CorrelationId)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_DroppingAgvPassedDoppelspielTriggerPoint_DropAt1050_OrderHasAlreadyBeenReported_DoesNotReportToConveyor()
    {
        var dropOrderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var dropHostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);
        var actionGroup = dropHostOrder.TryCreatePickDropActionGroup(OrderGenerator.KaeserConfiguration, dropOrderDetails.CreationDate);
        actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).State = ActionState.Started;

        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(dropHostOrder)
            .WithAgvHasLowBatteryLevel(false);
        await fixture.OrderRepository.StoreOrderReport(dropOrderDetails.Destination, dropHostOrder.CorrelationId);
        (await fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Destination, dropHostOrder.CorrelationId)).ShouldBeTrue();

        var droppingAgvPassedDoppelspielTriggerPoint = new DroppingAgvPassedDoppelspielTriggerPoint(actionGroup.Actions.First(a => a.ActionType == ActionType.Drop).Target.NodeId!,
            Guid.NewGuid(), dropHostOrder.CorrelationId, null, false, null);

        await fixture.TestHarness.Bus.Publish(droppingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();

        fixture.VerifyReportingToConveyors("PS03", dropHostOrder.Actions.First(a => a.ActionType == ActionType.Drop).Node, null, dropHostOrder.Hu, Times.Never());
        (await fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Source, dropHostOrder.CorrelationId)).ShouldBeFalse();
        (await fixture.OrderRepository.HasOrderBeenReported(dropOrderDetails.Destination, dropHostOrder.CorrelationId)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvStartedPick_OrderHasNotBeenReported_ReportOrder()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order);
        (await fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId)).ShouldBeFalse();

        await fixture.TestHarness.Bus.Publish(new AgvStartedPick("PS03", order.CorrelationId, "1020"));
        await fixture.ShouldHaveConsumedAsync<AgvStartedPick>();

        fixture.VerifyReportingToConveyors("PS03", null, "1020", null, Times.Once());
        (await fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId)).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_AgvStartedPick_OrderHasAlreadyBeenReported_DoesNotReportOrder()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order);
        await fixture.OrderRepository.StoreOrderReport("1020", order.CorrelationId);
        (await fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId)).ShouldBeTrue();

        await fixture.TestHarness.Bus.Publish(new AgvStartedPick("PS03", order.CorrelationId, "1020"));
        await fixture.ShouldHaveConsumedAsync<AgvStartedPick>();

        fixture.VerifyReportingToConveyors("PS03", null, "1020", null, Times.Never());
    }

    [TestMethod]
    public async Task Consume_AgvFinishedPick_OrderHasBeenReported_RemovesReport()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order);
        await fixture.OrderRepository.StoreOrderReport("1020", order.CorrelationId);
        (await fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId)).ShouldBeTrue();

        await fixture.TestHarness.Bus.Publish(new AgvFinishedPick(order.CorrelationId, "1020"));
        await fixture.ShouldHaveConsumedAsync<AgvFinishedPick>();
        (await fixture.OrderRepository.HasOrderBeenReported("1020", order.CorrelationId)).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AgvFinishedDrop_OrderHasBeenReported_RemovesReport()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order);
        await fixture.OrderRepository.StoreOrderReport(orderDetails.Destination, order.CorrelationId);
        (await fixture.OrderRepository.HasOrderBeenReported(orderDetails.Destination, order.CorrelationId)).ShouldBeTrue();

        await fixture.TestHarness.Bus.Publish(new AgvFinishedDrop(order.CorrelationId, orderDetails.Destination));
        await fixture.ShouldHaveConsumedAsync<AgvFinishedDrop>();
        (await fixture.OrderRepository.HasOrderBeenReported(orderDetails.Destination, order.CorrelationId)).ShouldBeFalse();
    }
}