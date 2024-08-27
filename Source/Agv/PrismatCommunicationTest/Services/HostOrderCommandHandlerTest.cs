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
    public async Task Consume_CreateHostOrder_DropOrderFor1050_QueueActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateShortCircuitOrder(pickAt1020:false);
   
        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<CreateHostOrder, QueueActionGroup>(new CreateHostOrder("Hu", order));
        fixture.VerifyHostOrderAdded();

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        var pickAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.Sequence.ShouldBe(0);
        dropAction.Sequence.ShouldBe(1);

        pickAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Source]);
        dropAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Destination]);
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_PickOrderFor1020NoPendingDropAt1050_QueueActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithDropOrdersHeadingTowardsShortLoopTriggerPoint(false)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);

        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<CreateHostOrder, QueueActionGroup>(new CreateHostOrder("Hu", order));
        fixture.VerifyHostOrderAdded();

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        var pickAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.Sequence.ShouldBe(0);
        dropAction.Sequence.ShouldBe(1);

        pickAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Source]);
        dropAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Destination]);
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_PickOrderFor1020PendingDropAt1050_DoesNotPublishQueueActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithDropOrdersHeadingTowardsShortLoopTriggerPoint(true)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));

        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
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

        moveAction.Target.NodeId.ShouldBe(OrderGenerator.DecisionNode);
        pickAction.Target.NodeId.ShouldBeEmpty();
        dropAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Destination]);
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
    public async Task Consume_CreateHostOrder_OrderWithUnknownDropTarget_NoDropOrdersHeadingTowardsCargoTriggerPoint_QueueActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrdersHeadingTowardsCargoTriggerPoint(false)
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

        pickAction.Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Source]);
        moveAction.Target.NodeId.ShouldBe(OrderGenerator.DecisionNode);
        dropAction.Target.NodeId.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownDropTarget_DropOrdersHeadingTowardsCargoTriggerPoint_HostOrderOnlySaved()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrdersHeadingTowardsCargoTriggerPoint(true)
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

        var command = new PassDecisionPoint(dropOrder.CorrelationId, Guid.NewGuid(), "a target node", false);
        await fixture.PublishAndGetFirstMessageAsync<PassDecisionPoint, DecisionPointPassed>(command);
        dropOrder.PassedTriggerPoint.ShouldBeTrue();
    }

    [TestMethod]
    public async Task PassDecisionPoint_DoesNotDropAt1060_DoesNotPublishDecisionPointPassed()
    {
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new PassDecisionPoint(order.CorrelationId, Guid.NewGuid(), "a target node", false);
        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<PassDecisionPoint>();
        await fixture.ShouldNotHavePublishedAsync<DecisionPointPassed>();

        order.PassedTriggerPoint.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_QueuePickDropActionGroupForAgv_AgvShouldPickNext_PublishesQueueActionGroupWithAssignedAgv()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new QueuePickDropActionGroupForAgv(order.CorrelationId, Guid.NewGuid(), "a target node", false);
        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<QueuePickDropActionGroupForAgv, QueueActionGroup>(command);

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AssignedAgv);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Target.NodeId.ShouldBe(command.PickNode);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Actions.Single(a => a.ActionType == ActionType.Drop).Node]);
    }

    [TestMethod]
    public async Task Consume_QueuePickDropActionGroupForAgv_AgvShouldNotPickNext_PublishesQueueActionGroupWithoutAssignedAgv()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new QueuePickDropActionGroupForAgv(order.CorrelationId, Guid.NewGuid(), "a target node", true);
        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<QueuePickDropActionGroupForAgv, QueueActionGroup>(command);

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(null);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Target.NodeId.ShouldBe(command.PickNode);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Actions.Single(a => a.ActionType == ActionType.Drop).Node]);
    }

    [TestMethod]
    public async Task Consume_PassCargoTriggerPoint_OrderIdProvided_UpdatesOrderAndPublishesPassCargoTriggerPointPassed()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new PassCargoTriggerPoint(Guid.NewGuid(), order.CorrelationId);

        var cargoTriggerPointPassed = await fixture.PublishAndGetFirstMessageAsync<PassCargoTriggerPoint, CargoTriggerPointPassed>(command);
        cargoTriggerPointPassed.Agv.ShouldBe(command.AgvCorrelationId);
        order.PassedTriggerPoint.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_PassCargoTriggerPoint_NoOrderIdProvided_DoesNotPublishCargoTriggerPointPassed()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostCommunicator();

        var command = new PassCargoTriggerPoint(Guid.NewGuid(), default(Guid));
       
        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<PassCargoTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<CargoTriggerPointPassed>();
    }

    [TestMethod]
    public async Task Consume_PassCargoTriggerPoint_NotDroppingAt970_DoesNotPublishCargoTriggerPointPassed()
    {
        var orderDetails = OrderGenerator.GenerateUnknownDropTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new PassCargoTriggerPoint(Guid.NewGuid(), default(Guid));

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<PassCargoTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<CargoTriggerPointPassed>();

        order.PassedTriggerPoint.ShouldBeFalse();
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
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move).Target.NodeId.ShouldBe(OrderGenerator.DecisionNode);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Actions.Single(a => a.ActionType == ActionType.Pick).Node]);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop).Target.NodeId.ShouldBeEmpty();
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

    [TestMethod]
    public async Task Consume_PassShortLoopTriggerPoint_ValidData_PublishesShortLoopTriggerPointPassed()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020:false);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order);

        var command = new PassShortLoopTriggerPoint(Guid.NewGuid(), order.CorrelationId, false);

        var shortLoopTriggerPointPassed = await fixture.PublishAndGetFirstMessageAsync<PassShortLoopTriggerPoint, ShortLoopTriggerPointPassed>(command);

        shortLoopTriggerPointPassed.Agv.ShouldBe(command.Agv);
        order.PassedTriggerPoint.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_PassShortLoopTriggerPoint_NotDropAt1050_DoesNotPublishShortLoopTriggerPointPassed()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order);

        var command = new PassShortLoopTriggerPoint(Guid.NewGuid(), order.CorrelationId, false);

        await fixture.TestHarness.Bus.Publish(command);
        
        await fixture.ShouldHaveConsumedAsync<PassShortLoopTriggerPoint>();
        await fixture.ShouldNotHavePublishedAsync<ShortLoopTriggerPointPassed>();
        order.PassedTriggerPoint.ShouldBeFalse();
    }
}