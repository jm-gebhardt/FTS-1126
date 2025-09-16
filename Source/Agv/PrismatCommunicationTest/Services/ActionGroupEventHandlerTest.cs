using Gebhardt.Acc.Common.Domain.Constants;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Services.EventHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class ActionGroupEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionGroupCompleted_ValidData_FinishHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var hostOrder = new HostOrder("OrderId", "hu", DateTime.UtcNow);
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());

        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        var actionGroupCompleted = new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCompleted);
        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();

        (await fixture.TestHarness.Published.Any<FinishHostOrder>(f =>
            f.Context.Message == new FinishHostOrder(actionGroup.CorrelationId, actionGroupCompleted.MessageCreatedDate))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionAborted_ValidDataNotTemporary_AbortHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionAborted = new ActionAborted(Guid.NewGuid(), new AgvAction() { ParentId = Guid.NewGuid() });

        await fixture.TestHarness.Bus.Publish(actionAborted);
        await fixture.ShouldHaveConsumedAsync<ActionAborted>();

        (await fixture.TestHarness.Published.Any<AbortHostOrder>(f =>
            f.Context.Message == new AbortHostOrder(actionAborted.Action.ParentId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCanceled_ValidData_AbortHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();

        var actionGroup = new ActionGroup();
        var actionAborted = new ActionGroupCanceled(actionGroup, actionGroup.CorrelationId, DateTime.Now);

        await fixture.TestHarness.Bus.Publish(actionAborted);
        await fixture.ShouldHaveConsumedAsync<ActionGroupCanceled>();

        (await fixture.TestHarness.Published.Any<AbortHostOrder>(f =>
            f.Context.Message == new AbortHostOrder(actionAborted.CorrelationId))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_ActionAborted_ValidDataTemporary_HostOrderNotAborted()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionAborted = new ActionAborted(Guid.NewGuid(), new AgvAction() { ParentId = Guid.NewGuid() }, true);

        await fixture.PublishMessageAsync(actionAborted);

        (await fixture.TestHarness.Published.Any<AbortHostOrder>(f =>
            f.Context.Message == new AbortHostOrder(actionAborted.Action.ParentId))).ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_ActionGroupCompleted_CreatorIsTransportOrderWebApi_NoFinishHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GenerateMoveActionGroup();

        var actionGroupCompleted = new ActionGroupCompleted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupCompleted);

        await fixture.ShouldHaveConsumedAsync<ActionGroupCompleted>();
        await fixture.ShouldNotHavePublishedAsync<FinishHostOrder>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupStarted_ValidData_HandleHostOrderStart()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: Guid.NewGuid());

        var actionGroupStarted = new ActionGroupStarted(actionGroup, actionGroup.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(actionGroupStarted);

        await fixture.ShouldHaveConsumedAsync<ActionGroupStarted>();
        (await fixture.TestHarness.Published.Any<HandleHostOrderStart>(f =>
            f.Context.Message == new HandleHostOrderStart(actionGroup.CorrelationId, actionGroup.AssignedAgv!.Value, actionGroupStarted.MessageCreatedDate))).ShouldBeTrue();
    }

    [TestMethod]
    public async Task PickActionCompleted_ValidData_PublishesFinishPick()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        order.Start(DateTime.UtcNow, Guid.NewGuid());
        HostOrderConsumerTestFixture<ActionGroupEventHandler> fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>()
            .WithHostOrders([order]);

        var pickActionCompleted = new PickActionCompleted(order.CorrelationId, order.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, DateTime.UtcNow);

        var finishPick = await fixture.PublishAndGetFirstMessageAsync<PickActionCompleted, FinishPick>(pickActionCompleted);

        finishPick.ActionCorrelationId.ShouldBe(pickActionCompleted.ActionCorrelationId);
        finishPick.CorrelationId.ShouldBe(pickActionCompleted.CorrelationId);
        finishPick.Timestamp.ShouldBe(pickActionCompleted.Timestamp);
    }

    [TestMethod]
    public async Task DroppingAgvPassedDoppelspielTriggerPoint_ValidData_PublishesPassTriggerPoint()
    {
        var agvPassedDoppelspielTriggerPoint = new DroppingAgvPassedDoppelspielTriggerPoint("drop target of action".ToGraphNode(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, "pickNode".ToGraphNode());

        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();

        await fixture.TestHarness.Bus.Publish(agvPassedDoppelspielTriggerPoint);

        await fixture.ShouldHaveConsumedAsync<DroppingAgvPassedDoppelspielTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<PassTriggerPoint>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupInvalid_RouteValidationError_PublishesCancelInvalidHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var orderEvent = new ActionGroupInvalid(actionGroup.CorrelationId, actionGroup, [HostOrderFailureReasons.ThereMustBeAtLeastOneAgvThatKnowsTheCompleteRouteOfAllActionsWithinTheGivenActionGroup]);

        await fixture.TestHarness.Bus.Publish(orderEvent);

        await fixture.ShouldHaveConsumedAsync<ActionGroupInvalid>();
        await fixture.ShouldHavePublishedAsync<CancelInvalidHostOrder>();
    }

    [TestMethod]
    public async Task Consume_ActionGroupInvalid_NotRouteValidationError_PublishesCancelInvalidHostOrder()
    {
        var fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>();
        var actionGroup = AgvActionGenerator.GeneratePickDropActionGroup();
        var orderEvent = new ActionGroupInvalid(actionGroup.CorrelationId, actionGroup, ["other validation error"]);

        await fixture.TestHarness.Bus.Publish(orderEvent);

        await fixture.ShouldHaveConsumedAsync<ActionGroupInvalid>();
        await fixture.ShouldNotHavePublishedAsync<CancelInvalidHostOrder>();
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_DropAtAkl_PublishesTryQueueNextAklPick()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        order.Start(DateTime.UtcNow, Guid.NewGuid());
        HostOrderConsumerTestFixture<ActionGroupEventHandler> fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>()
            .WithHostOrders([order]);

        var orderEvent = new PickActionCompleted(order.CorrelationId, order.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(orderEvent);

        await fixture.ShouldHaveConsumedAsync<PickActionCompleted>();
        await fixture.ShouldHavePublishedAsync<TryQueueNextAklPick>();
        await fixture.ShouldHavePublishedAsync<FinishPick>();
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_PickAtAkl_DoesNotPublishTryQueueNextAklPick()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        order.Start(DateTime.UtcNow, Guid.NewGuid());
        HostOrderConsumerTestFixture<ActionGroupEventHandler> fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>()
            .WithHostOrders([order]);

        var orderEvent = new PickActionCompleted(order.CorrelationId, order.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(orderEvent);

        await fixture.ShouldHaveConsumedAsync<PickActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<TryQueueNextAklPick>();
        await fixture.ShouldHavePublishedAsync<FinishPick>();
    }

    [TestMethod]
    public async Task Consume_PickActionCompleted_LongLoopOrder_DoesNotPublishTryQueueNextAklPick()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        order.Start(DateTime.UtcNow, Guid.NewGuid());
        HostOrderConsumerTestFixture<ActionGroupEventHandler> fixture = new HostOrderConsumerTestFixture<ActionGroupEventHandler>()
            .WithHostOrders([order]);

        var orderEvent = new PickActionCompleted(order.CorrelationId, order.Actions.Single(a => a.ActionType == ActionType.Pick).CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(orderEvent);

        await fixture.ShouldHaveConsumedAsync<PickActionCompleted>();
        await fixture.ShouldNotHavePublishedAsync<TryQueueNextAklPick>();
        await fixture.ShouldHavePublishedAsync<FinishPick>();
    }
}