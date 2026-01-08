using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.OrderTranslator.Test.Helper;

namespace Gebhardt.Acc.OrderTranslator.Test.Services.EventHandler;

[TestClass]
public class ConveyorReportingEventHandlerTest
{
    [TestMethod]
    public async Task Consume_DroppingAgvPassedDoppelspielTriggerPoint_TargetNeedsPalletReporting_PublishesOrderReportingRequested()
    {
        var dropHostOrder = OrderGenerator.GenerateOrder(OrderGenerator.Apl1, OrderGenerator.Ps01);
        var nextPickOrder = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var actionGroup = dropHostOrder.CreateActionGroup([OrderGenerator.Apl1], [OrderGenerator.Ps01]);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(dropHostOrder, nextPickOrder)
            .WithStation(OrderGenerator.Ps01);

        var droppingAgvPassedDoppelspielTriggerPoint = new DroppingAgvPassedDoppelspielTriggerPoint(actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!,
            Guid.NewGuid(), dropHostOrder.CorrelationId, nextPickOrder.CorrelationId);

        await fixture.TestHarness.Bus.Publish(droppingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<OrderReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_DroppingAgvPassedDoppelspielTriggerPoint_TargetDoesNotNeedNeedPalletReporting_DoesNotPublishOrderReportingRequested()
    {
        var dropHostOrder = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var actionGroup = dropHostOrder.CreateActionGroup([OrderGenerator.Ps01], [OrderGenerator.Apl1]);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(dropHostOrder)
            .WithStation(OrderGenerator.Apl1);

        var droppingAgvPassedDoppelspielTriggerPoint = new DroppingAgvPassedDoppelspielTriggerPoint(actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!,
            Guid.NewGuid(), dropHostOrder.CorrelationId, null);

        await fixture.TestHarness.Bus.Publish(droppingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<OrderReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_DroppingAgvPassedDoppelspielTriggerPoint_TargetStationUnknown_DoesNotPublishOrderReportingRequested()
    {
        var dropHostOrder = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var actionGroup = dropHostOrder.CreateActionGroup([OrderGenerator.Ps01], [OrderGenerator.Apl1]);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(dropHostOrder)
            .WithStation(null);

        var droppingAgvPassedDoppelspielTriggerPoint = new DroppingAgvPassedDoppelspielTriggerPoint(actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!,
            Guid.NewGuid(), dropHostOrder.CorrelationId, null);

        await fixture.TestHarness.Bus.Publish(droppingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<OrderReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_PickingAgvPassedDoppelspielTriggerPoint_TargetNeedsPalletReporting_PublishesOrderReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var actionGroup = order.CreateActionGroup([OrderGenerator.Ps01], [OrderGenerator.Apl1]);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(OrderGenerator.Ps01);

        var pickingAgvPassedDoppelspielTriggerPoint = new PickingAgvPassedDoppelspielTriggerPoint(actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!, Guid.NewGuid(), order.CorrelationId);

        await fixture.TestHarness.Bus.Publish(pickingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<OrderReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_PickingAgvPassedDoppelspielTriggerPoint_TargetDoesNotNeedNeedPalletReporting_DoesNotPublishOrderReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Apl1, OrderGenerator.Ps01);
        var actionGroup = order.CreateActionGroup([OrderGenerator.Apl1], [OrderGenerator.Ps01]);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(OrderGenerator.Apl1);

        var pickingAgvPassedDoppelspielTriggerPoint = new PickingAgvPassedDoppelspielTriggerPoint(actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!, Guid.NewGuid(), order.CorrelationId);

        await fixture.TestHarness.Bus.Publish(pickingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<OrderReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_PickingAgvPassedDoppelspielTriggerPoint_TargetStationUnknown_DoesNotPublishOrderReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var actionGroup = order.CreateActionGroup([OrderGenerator.Ps01], [OrderGenerator.Apl1]);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(null);

        var pickingAgvPassedDoppelspielTriggerPoint = new PickingAgvPassedDoppelspielTriggerPoint(actionGroup!.Actions.First(a => a.ActionType == ActionType.Drop).Target.Node!, Guid.NewGuid(), order.CorrelationId);

        await fixture.TestHarness.Bus.Publish(pickingAgvPassedDoppelspielTriggerPoint);
        await fixture.ShouldHaveConsumedAsync<PickingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<OrderReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_AgvStartedPick_TargetNeedsPalletReporting_PublishesOrderReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
             .WithHostOrders(order)
             .WithStation(OrderGenerator.Ps01);

        await fixture.PublishMessageAsync(new AgvStartedPick(OrderGenerator.Ps01.Name, order.CorrelationId));

        await fixture.ShouldHavePublishedAsync<OrderReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_AgvStartedPick_UnknownStation_DoesNotPublishOrderReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(null);

        await fixture.PublishMessageAsync(new AgvStartedPick(OrderGenerator.Ps01.Name, order.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<OrderReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_AgvStartedPick_TargetDoesNotNeedPalletReporting_DoesNotPublishOrderReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Apl1, OrderGenerator.Ps01);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(OrderGenerator.Apl1);

        await fixture.PublishMessageAsync(new AgvStartedPick(OrderGenerator.Apl1.Name, order.CorrelationId));

        await fixture.ShouldNotHavePublishedAsync<OrderReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_TargetNeedsPalletReporting_AutomaticOrder_PublishesPalletDropReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Apl1, OrderGenerator.Ps01);
        var actionGroup = order.CreateActionGroup([OrderGenerator.Apl1], [OrderGenerator.Ps01]);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(OrderGenerator.Ps01);

        await fixture.PublishMessageAsync(new DropActionCompleted(order.CorrelationId, order.Drop!.CorrelationId, actionGroup!, DateTime.UtcNow));

        await fixture.ShouldHavePublishedAsync<PalletDropReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_TargetNeedsPalletReporting_ManualOrder_WithSap_PublishesPalletDropReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Apl1, OrderGenerator.Ps01);
        var actionGroup = OrderGenerator.GetActionGroupFromOrder(order, OrderGenerator.Apl1, OrderGenerator.Ps01, true, true);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(OrderGenerator.Ps01);

        await fixture.PublishMessageAsync(new DropActionCompleted(order.CorrelationId, order.Drop!.CorrelationId, actionGroup, DateTime.UtcNow));

        await fixture.ShouldHavePublishedAsync<PalletDropReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_TargetNeedsPalletReporting_ManualOrder_NoSap_DoesNotPublishPalletDropReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Apl1, OrderGenerator.Ps01);
        var actionGroup = OrderGenerator.GetActionGroupFromOrder(order, OrderGenerator.Apl1, OrderGenerator.Ps01, true);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(OrderGenerator.Ps01);

        await fixture.PublishMessageAsync(new DropActionCompleted(order.CorrelationId, order.Drop!.CorrelationId, actionGroup, DateTime.UtcNow));

        await fixture.ShouldNotHavePublishedAsync<PalletDropReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_DropActionCompleted_TargetDoesNotNeedPalletReporting_AutomaticOrder_DoesNotPublishPalletDropReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Ps01, OrderGenerator.Apl1);
        var actionGroup = order.CreateActionGroup([OrderGenerator.Ps01], [OrderGenerator.Apl1]);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(OrderGenerator.Apl1);

        await fixture.PublishMessageAsync(new DropActionCompleted(order.CorrelationId, order.Drop!.CorrelationId, actionGroup!, DateTime.UtcNow));

        await fixture.ShouldNotHavePublishedAsync<PalletDropReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_ManualActionPassedDoppelspielTrigger_TargetDoesNotNeedPalletReporting_DoesNotPublishOrderReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Apl1, OrderGenerator.Ps01);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(OrderGenerator.Apl1);

        await fixture.TestHarness.Bus.Publish(new ManualActionPassedDoppelspielTrigger(OrderGenerator.Apl1.InputLanes.First(), OrderGenerator.Ap01.Name, true, order.Hu, order.CorrelationId, Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<ManualActionPassedDoppelspielTrigger>();

        await fixture.ShouldNotHavePublishedAsync<OrderReportingRequested>();
    }

    [TestMethod]
    public async Task Consume_ManualActionPassedDoppelspielTrigger_TargetNeedsPalletReporting_PublishesOrderReportingRequested()
    {
        var order = OrderGenerator.GenerateOrder(OrderGenerator.Apl1, OrderGenerator.Ps01);
        var fixture = new ConveyorReportingEventHandlerTestFixture()
            .WithHostOrders(order)
            .WithStation(OrderGenerator.Ps01);

        await fixture.TestHarness.Bus.Publish(new ManualActionPassedDoppelspielTrigger(OrderGenerator.Ps01.InputLanes.First(), OrderGenerator.Ps01.Name, true, order.Hu, order.CorrelationId, Guid.NewGuid()));
        await fixture.ShouldHaveConsumedAsync<ManualActionPassedDoppelspielTrigger>();

       await fixture.ShouldHavePublishedAsync<OrderReportingRequested>();
    }
}