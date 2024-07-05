using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using Gebhardt.Acc.PrismatCommunication.Messages.Events;
using Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
public class HostOrderCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_CreateHostOrder_ValidData_CreatesOrder()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostCommunicator();

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        await fixture.TestHarness.Bus.Publish( new CreateHostOrder("Hu", orderDetails));

        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        fixture.VerifyHostOrderAdded();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithKnownPickAndDropTarget_QueueActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateShortCircuitOrder();
   
        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<CreateHostOrder, QueueActionGroup>(new CreateHostOrder("Hu", order));
        fixture.VerifyHostOrderAdded();

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        var pickAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.Sequence.ShouldBe(0);
        dropAction.Sequence.ShouldBe(1);

        pickAction.TargetNodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Source]);
        dropAction.TargetNodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Destination]);
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownPickTarget_NoDropOrdersHeadingTowardsDecisionPoint_QueueActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrdersHeadingTowardsDecisionPoint(false)
            .WithHostOrderRepository(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownPickTargetOrder();

        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<CreateHostOrder, QueueActionGroup>(new CreateHostOrder("Hu", order));
        fixture.VerifyHostOrderAdded();

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        var pickAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);
        var moveAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

        moveAction.Sequence.ShouldBe(0);
        pickAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        moveAction.TargetNodeId.ShouldBe(OrderGenerator.DecisionNode);
        pickAction.TargetNodeId.ShouldBeEmpty();
        dropAction.TargetNodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Destination]);
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownPickTarget_DropOrdersHeadingTowardsDecisionPoint_HostOrderOnlySaved()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrdersHeadingTowardsDecisionPoint(true)
            .WithHostOrderRepository(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownPickTargetOrder();

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));
        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        fixture.VerifyHostOrderAdded();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownDropTarget_NoDropOrdersHeadingTowardsLoadTriggerPoint_QueueActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrdersHeadingTowardsLoadTriggerPoint(false)
            .WithHostOrderRepository(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownDropTargetOrder();

        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<CreateHostOrder, QueueActionGroup>(new CreateHostOrder("Hu", order));
        fixture.VerifyHostOrderAdded();

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        var pickAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);
        var moveAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

        pickAction.Sequence.ShouldBe(0);
        moveAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        pickAction.TargetNodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Source]);
        moveAction.TargetNodeId.ShouldBe(OrderGenerator.DecisionNode);
        dropAction.TargetNodeId.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownDropTarget_DropOrdersHeadingTowardsLoadTriggerPoint_HostOrderOnlySaved()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrdersHeadingTowardsLoadTriggerPoint(true)
            .WithHostOrderRepository(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownDropTargetOrder();

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));
        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        fixture.VerifyHostOrderAdded();
    }

    [TestMethod]
    public async Task Consume_FinishHostOrder_ValidData_ReportOrderCompletion()
    {
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId", "Hu", DateTime.UtcNow);
        hostOrder.AddAction("node", ActionType.Move);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, DateTime.UtcNow));
        await fixture.ShouldHaveConsumedAsync<FinishHostOrder>();
        fixture.VerifyReportOrderCompletion();
    }

    [TestMethod]
    public async Task Consume_FinishHostOrder_InformationOutdated_NoReportOrderCompletion()
    {
        var now = DateTime.UtcNow;
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId","Hu", DateTime.UtcNow);
        hostOrder.Finish(now.AddDays(1));

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, now));
        await fixture.ShouldHaveConsumedAsync<FinishHostOrder>();
        fixture.VerifyReportOrderCompletion(Times.Never());
    }

    [TestMethod]
    public async Task PassDecisionPoint_ValidData_PublishesDecisionPointPassed()
    {
        var dropOrderDetails = OrderGenerator.GenerateUnknownDropTargetOrder();
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(dropOrderDetails);

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(dropOrder)
            .WithHostCommunicator();

        var command = new PassDecisionPoint(dropOrder.CorrelationId, Guid.NewGuid(), "a target node");
        await fixture.PublishAndGetFirstMessageAsync<PassDecisionPoint, DecisionPointPassed>(command);
        dropOrder.PassedTriggerPoint.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_QueuePickDropActionGroupForAgv_ValidData_PublishesQueueActionGroup()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new QueuePickDropActionGroupForAgv(order.CorrelationId, Guid.NewGuid(), "a target node");
        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<QueuePickDropActionGroupForAgv, QueueActionGroup>(command);

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AssignedAgv);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).TargetNodeId.ShouldBe(command.PickNode);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).TargetNodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Actions.Single(a => a.ActionType == ActionType.Drop).Node]);
    }

    [TestMethod]
    public async Task Consume_PassLoadTriggerPoint_OrderIdProvided_UpdatesOrderAndPublishesLoadTriggerPointPassed()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new PassLoadTriggerPoint(Guid.NewGuid(), order.CorrelationId);

        var loadTriggerPointPassed = await fixture.PublishAndGetFirstMessageAsync<PassLoadTriggerPoint, LoadTriggerPointPassed>(command);
        loadTriggerPointPassed.Agv.ShouldBe(command.AgvCorrelationId);
        order.PassedTriggerPoint.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_PassLoadTriggerPoint_NoOrderIdProvided_LoadTriggerPointPassed()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostCommunicator();

        var command = new PassLoadTriggerPoint(Guid.NewGuid(), default(Guid));

        var loadTriggerPointPassed = await fixture.PublishAndGetFirstMessageAsync<PassLoadTriggerPoint, LoadTriggerPointPassed>(command);
        loadTriggerPointPassed.Agv.ShouldBe(command.AgvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_QueueActionGroupWithUnknownDropTargetForAgv_ValidData_PublishesQueueActionGroup()
    {
        var orderDetails = OrderGenerator.GenerateUnknownDropTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new QueueActionGroupWithUnknownDropTargetForAgv(order.CorrelationId, Guid.NewGuid());
        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<QueueActionGroupWithUnknownDropTargetForAgv, QueueActionGroup>(command);

        order.AssignedAgv.ShouldBe(command.Agv);
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).TargetNodeId.ShouldBe(OrderGenerator.DecisionNode);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).TargetNodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Actions.Single(a => a.ActionType == ActionType.Pick).Node]);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).TargetNodeId.ShouldBeEmpty();
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.Agv);
    }

    [TestMethod]
    public async Task Consume_FinishPick_ValidData_PickCompletionReported()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var pickAction = order.Actions.Single(a => a.ActionType == ActionType.Pick);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new FinishPick(order.CorrelationId, pickAction.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<FinishPick>();
        fixture.VerifyPickCompletion(Times.Once());
    }

    [TestMethod]
    public async Task Consume_FinishPick_UnknownOrder_PickCompletionNotReported()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var pickAction = order.Actions.Single(a => a.ActionType == ActionType.Pick);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostCommunicator();

        var command = new FinishPick("Unknown".ToGuid(), pickAction.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<FinishPick>();
        fixture.VerifyPickCompletion(Times.Never());
    }

    [TestMethod]
    public async Task Consume_FinishPick_NotPickAction_PickCompletionNotReported()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        var dropAction = order.Actions.Single(a => a.ActionType == ActionType.Drop);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new FinishPick(order.CorrelationId, dropAction.CorrelationId, DateTime.UtcNow);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<FinishPick>();
        fixture.VerifyPickCompletion(Times.Never());
    }
}