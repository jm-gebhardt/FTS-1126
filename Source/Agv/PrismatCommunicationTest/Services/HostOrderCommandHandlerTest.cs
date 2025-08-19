using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
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
            .WithHostOrderWithSameHu(null)
            .WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(null)
            .WithinAklAgvLimit(true)
            .WithValidationError(null)
            .WithHostCommunicator();

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", orderDetails));

        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        fixture.VerifyHostOrderAdded();
        fixture.VerifyReportOrderError("LeNo123", "SRC1", "DST1", KaeserGraphConfiguration.ErrorCodeTargetUnknown, Times.Never());
        fixture.VerifyReportOrderError("LeNo123", "SRC1", "DST1", KaeserGraphConfiguration.ErrorCodeSourceUnknown, Times.Never());
    }

    [DataRow(KaeserGraphConfiguration.ErrorCodeTargetUnknown)]
    [DataRow(KaeserGraphConfiguration.ErrorCodeSourceUnknown)]
    [DataTestMethod]
    public async Task Consume_CreateHostOrder_ValidationError_CreatesOrder(int errorCode)
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithValidationError((errorCode, "corresponding error info"))
            .WithHostCommunicator();

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", orderDetails));

        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        fixture.VerifyHostOrderAdded();
        fixture.VerifyReportOrderError("Hu", "SRC1", "DST1", errorCode, Times.Once());
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_RepeatedOrderForSameHuNoRelatedOrder_CreatesOrderPublishesCancelOrAbortActionGroup()
    {
        const string hu = "hu";
        var now = DateTime.UtcNow;
        var createdDate = now.AddMinutes(-2);

        var unfinishedOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder(hu), createdDate.AddMinutes(-2));
        var orderDetails = OrderGenerator.GenerateUnknownDropTargetOrder(hu);

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(unfinishedOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder(hu, orderDetails));

        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
        fixture.VerifyHostOrderAdded();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_DropOrderFor1050PickAtAkl_CreateActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithinAklAgvLimit(true)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);

        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<CreateHostOrder, CreateActionGroup>(new CreateHostOrder("Hu", order));
        fixture.VerifyHostOrderAdded();

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_DropOrderFor1050_StationGroupOccupied_CreateActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithinAklAgvLimit(false)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));

        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        fixture.VerifyHostOrderAdded();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_DropOrderFor1050_StationGroupWillBeOccupied_PublishesCreateActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithinAklAgvLimit(false)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));

        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        fixture.VerifyHostOrderAdded();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_PickOrderFor1020NoPendingDropAt1050_StationGroupNotOccupied_QueueActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(null)
            .WithValidationError(null)
            .WithinAklAgvLimit(true)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);

        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<CreateHostOrder, QueueActionGroup>(new CreateHostOrder("Hu", order));
        fixture.VerifyHostOrderAdded();

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        var pickAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);

        pickAction.InfoToAgv?.TryGetValue(InfoToAgvKey.CargoRotation, out string? _).ShouldBeTrue();
        pickAction.InfoToAgv?.TryGetValue(InfoToAgvKey.LiftHeight, out string? _).ShouldBeFalse();
        dropAction.InfoToAgv?.TryGetValue(InfoToAgvKey.LiftHeight, out string? _).ShouldBeTrue();
        dropAction.InfoToAgv?.TryGetValue(InfoToAgvKey.ForceDroppingBackwards, out string? _).ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_PickOrderFor1020NoPendingDropAt1050_StationGroupOccupied_PublishesCreateActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(null)
            .WithValidationError(null)
            .WithinAklAgvLimit(false)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));

        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
        fixture.VerifyHostOrderAdded();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_PickOrderFor1020NoPendingDropAt1050_StationGroupWillBeOccupied_PublishesCreateActionGroup()
    {
        var order = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithHostOrders([])
            .WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(null)
            .WithValidationError(null)
            .WithinAklAgvLimit(false)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));

        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
        fixture.VerifyHostOrderAdded();
    }

    [TestMethod]
    public async Task Consume_CreateHostOrder_PickOrderFor1020PendingDropAt1050_PublishesCreateActionGroup()
    {
        var order = OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true);
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(Guid.NewGuid())
            .WithHostOrderWithSameHu(null)
            .WithHostOrders([dropOrder])
            .WithValidationError(null)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));

        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
    }

    [DataRow(0)] //1020
    [DataRow(1)] //1040
    [DataTestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownPickTarget_NoDropOrdersHeadingTowardsDoppelSpielTriggerPoint_QueueActionGroup(int input)
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(null)
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownPickTargetOrder(pickTarget: input);

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

    [DataRow(0)] //1020
    [DataRow(1)] //1040
    [DataTestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownPickTarget_DropOrdersHeadingTowardsDoppelSpielTriggerPoint_PublishesCreateActionGroup(int input)
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(Guid.NewGuid())
            .WithHostOrders([dropOrder])
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownPickTargetOrder(pickTarget: input);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));
        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
        fixture.VerifyHostOrderAdded();
    }

    [DataRow(0)] //1050
    [DataRow(1)] //1060
    [DataTestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownDropTarget_NoDropOrdersHeadingTowardsDoppelSpielTriggerPoint_QueueActionGroup(int output)
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(null)
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownDropTargetOrder(dropTarget: output);

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

    [DataRow(0)] //1050
    [DataRow(1)] //1060
    [DataTestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownDropTarget_DropOrdersHeadingTowardsDoppelSpielTriggerPoint_QueueIsEmpty_PublishesCreateActionGroup(int output)
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(Guid.NewGuid())
            .WithHostOrders([dropOrder])
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithHostCommunicator();
        fixture.Configuration.SetQueueIsOccupied(false);

        var order = OrderGenerator.GenerateUnknownDropTargetOrder(dropTarget: output);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));
        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
        fixture.VerifyHostOrderAdded();
    }

    [DataRow(0)] //1050
    [DataRow(1)] //1060
    [DataTestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownDropTarget_DropOrdersHeadingTowardsDoppelSpielTriggerPoint_QueueIsNotEmpty_QueueActionGroup(int output)
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrderHeadingTowardsDoppelSpielTriggerPoint(dropOrder.CorrelationId)
            .WithHostOrders([dropOrder])
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithHostCommunicator();
        fixture.Configuration.SetQueueIsOccupied(true);

        var order = OrderGenerator.GenerateUnknownDropTargetOrder(dropTarget: output);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));
        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldNotHavePublishedAsync<CreateActionGroup>();
        fixture.VerifyHostOrderAdded();
    }

    [TestMethod]
    public async Task Consume_FinishHostOrder_ValidData_ReportOrderCompletion()
    {
        var hostOrder = new HostOrder("OrderId", "Hu", DateTime.UtcNow);
        hostOrder.AddAction("node", ActionType.Pick);
        hostOrder.AddAction("node", ActionType.Drop);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, DateTime.UtcNow, null));
        await fixture.ShouldHaveConsumedAsync<FinishHostOrder>();
        fixture.VerifyReportOrderCompletion(hostOrder.Actions.OrderBy(a => a.SequenceNumber).Last().Node);
    }

    [TestMethod]
    public async Task Consume_FinishHostOrder_InformationOutdated_NoReportOrderCompletion()
    {
        var now = DateTime.UtcNow;
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        hostOrder.Finish(now.AddDays(1));

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, now, null));
        await fixture.ShouldHaveConsumedAsync<FinishHostOrder>();
        fixture.VerifyReportOrderCompletion(times: Times.Never());
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
        pickAction.FinishedDate.ShouldBe(command.Timestamp);
    }

    [TestMethod]
    public async Task Consume_FinishPick_LongLoop_PickCompletionReported()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
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
    public async Task Consume_FinishPick_LongLoopUpdatedMoveFinished_PickCompletionReported()
    {
        var orderDetails = OrderGenerator.GenerateUnknownPickTargetOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var actionGroup = order.CreateActionGroup(fixture.Configuration);
        var pickAction = actionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

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
    public async Task Consume_CancelInvalidHostOrder_ValidData_ReportOrderError()
    {
        var orderDetails = OrderGenerator.GenerateShortCircuitOrder();
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(orderDetails);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(order)
            .WithHostCommunicator();

        var command = new CancelInvalidHostOrder(order.CorrelationId);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<CancelInvalidHostOrder>();
        fixture.VerifyReportOrderError(order.Hu, order.Actions.First(a => a.ActionType == ActionType.Pick).Node, order.Actions.First(a => a.ActionType == ActionType.Drop).Node, KaeserGraphConfiguration.ErrorCodeNoRoute, Times.Once());
    }

    [TestMethod]
    public async Task Consume_PassTriggerPoint_ShouldPickNext_ShortCircuit_PublishesQueueActionGroup()
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());

        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        dropOrder.RegisterForDoppelspiel();
        nextPickOrder.RegisterForDoppelspiel();
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([dropOrder, nextPickOrder]);

        var command = new PassTriggerPoint(Guid.NewGuid(), dropOrder.CorrelationId, nextPickOrder.CorrelationId, true, "pickNode");

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<PassTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Target.NodeId.ShouldBe(command.PickNode);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AgvCorrelationId);
    }

    [DataRow(0)] //1020
    [DataRow(1)] //1040
    [DataTestMethod]
    public async Task Consume_PassTriggerPoint_ShouldPickNext_LongCircuitWE_PublishesQueueActionGroup(int input)
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());

        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder(pickTarget: input));
        dropOrder.RegisterForDoppelspiel();
        nextPickOrder.RegisterForDoppelspiel();
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([dropOrder, nextPickOrder]);

        var command = new PassTriggerPoint(Guid.NewGuid(), dropOrder.CorrelationId, nextPickOrder.CorrelationId, true, "pickNode");

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<PassTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Target.NodeId.ShouldBe(command.PickNode);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AgvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_PassTriggerPoint_ShouldPickNext_LongCircuitAPL_PublishesQueueActionGroup()
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        dropOrder.RegisterForDoppelspiel();
        nextPickOrder.RegisterForDoppelspiel();
        string pickNode = nextPickOrder.Actions
            .Single(a => a.ActionType == ActionType.Pick).Node;
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([dropOrder, nextPickOrder]);

        var command = new PassTriggerPoint(Guid.NewGuid(), dropOrder.CorrelationId, nextPickOrder.CorrelationId, true, pickNode);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<PassTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick)
            .Target.NodeId.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[pickNode]);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AgvCorrelationId);
    }

    [TestMethod]
    public async Task Consume_PassTriggerPoint_ShouldNotPickNext_DoppelspielPartner_PublishesQueueActionGroup()
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        dropOrder.RegisterForDoppelspiel();
        nextPickOrder.RegisterForDoppelspiel();
        string pickNode = nextPickOrder.Actions
            .Single(a => a.ActionType == ActionType.Pick).Node;
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([dropOrder])
            .WithUnMatchedPickOrder(nextPickOrder);

        var command = new PassTriggerPoint(Guid.NewGuid(), dropOrder.CorrelationId, nextPickOrder.CorrelationId, false, pickNode);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<PassTriggerPoint>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_AbortHostOrder_HasNotQueuedDoppelspielpartner_PublishesQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        var doppelspielPartner = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.RegisterForDoppelspiel();
        doppelspielPartner.RegisterForDoppelspiel();
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithUnMatchedPickOrder(doppelspielPartner)
            .WithinAklAgvLimit(true);

        var command = new AbortHostOrder(hostOrder.CorrelationId);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<AbortHostOrder>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        hostOrder.CanceledDate.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_AbortHostOrder_HasNotQueuedDoppelspielpartner_StationIsOccupied_OrderLikelyReportedToConveyor_PublishesQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));

        var doppelspielPartner = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        hostOrder.RegisterForDoppelspiel();
        doppelspielPartner.RegisterForDoppelspiel();
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithUnMatchedPickOrder(doppelspielPartner)
            .WithinAklAgvLimit(false);

        var command = new AbortHostOrder(hostOrder.CorrelationId);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<AbortHostOrder>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        hostOrder.CanceledDate.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_AbortHostOrder_HasNotQueuedDoppelspielpartner_StationIsOccupied_AklAgvLimitUnused_PublishesQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));

        var doppelspielPartner = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        hostOrder.RegisterForDoppelspiel();
        doppelspielPartner.RegisterForDoppelspiel();
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithUnMatchedPickOrder(doppelspielPartner)
            .WithinAklAgvLimit(true);

        var command = new AbortHostOrder(hostOrder.CorrelationId);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<AbortHostOrder>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        hostOrder.CanceledDate.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_AbortHostOrder_NoDoppelspielpartner_UpdatesOrder()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder]);

        var command = new AbortHostOrder(hostOrder.CorrelationId);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<AbortHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        hostOrder.CanceledDate.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_TryQueueDelayedOrder_StationIsOccupied_DoesNotPublishQueueHostOrder()
    {
        var nextOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithNextHostOrder(nextOrder)
            .WithinAklAgvLimit(false);

        var command = new TryQueueDelayedOrder(fixture.Configuration.AklDoppelspielPickTargets[nextOrder.Target], nextOrder.Target);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryQueueDelayedOrder>();

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryQueueDelayedOrder_StationIsOccupied_AklAgvLimitNotUsed_PublishesQueueHostOrder()
    {
        var nextOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithNextHostOrder(nextOrder)
            .WithinAklAgvLimit(true);

        var command = new TryQueueDelayedOrder(fixture.Configuration.DoppelspielPickTargets[nextOrder.Target].First(), nextOrder.Target);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryQueueDelayedOrder>();

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryQueueDelayedOrder_NoNextOrderForStationGroup_DoesNotPublishQueueHostOrder()
    {
        var nextOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithinAklAgvLimit(true);

        var command = new TryQueueDelayedOrder(fixture.Configuration.DoppelspielPickTargets[nextOrder.Target].First(), nextOrder.Target);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryQueueDelayedOrder>();

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryQueueDelayedOrder_ValidData_PublishesQueueHostOrder()
    {
        var nextOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithNextHostOrder(nextOrder)
            .WithinAklAgvLimit(true);

        var command = new TryQueueDelayedOrder(fixture.Configuration.AklDoppelspielPickTargets[nextOrder.Target], nextOrder.Target);
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryQueueDelayedOrder>();

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ResetDoppelspielPartner_ValidData_ResetsDoppelspiel()
    {
        var orderId = Guid.NewGuid();
        var partnerOrderId = Guid.NewGuid();
        List<HostOrder> orders =
        [
            OrderGenerator.GenerateHostOrderFromOrderDetails(new OrderDetails(orderId, "HU", "A", "B", "leNo", HostOrderCreator.Wms, DateTime.UtcNow)),
            OrderGenerator.GenerateHostOrderFromOrderDetails(new OrderDetails(partnerOrderId, "HU", "B", "A", "leNo", HostOrderCreator.Wms, DateTime.UtcNow))
        ];

        orders[0].RegisterForDoppelspiel();
        orders[1].RegisterForDoppelspiel();
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders(orders);

        var command = new ResetDoppelspielPartner(orderId);

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<ResetDoppelspielPartner>();
        fixture.VerifyDoppelspielIsNull(Times.Exactly(1));
    }

    [TestMethod]
    public async Task Consume_TryQueueNextAklPick_ValidData_QueueActionGroup()
    {
        var nextOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithNextHostOrder(nextOrder)
            .WithinAklAgvLimit(true);

        var command = new TryQueueNextAklPick(nextOrder.Source, Guid.NewGuid());

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryQueueNextAklPick>();

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AssignedAgv);
    }

    [TestMethod]
    public async Task Consume_TryQueueNextAklPick_NoNextOrder_DoesNotQueueActionGroup()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithinAklAgvLimit(true);

        var command = new TryQueueNextAklPick("2130", Guid.NewGuid());

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryQueueNextAklPick>();

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielpartnerQueueing_HasAlreadyQueueDoppelspielPartner_RespondsDoppelspielPartnerQueued()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        hostOrder.RegisterForDoppelspiel();
        hostOrder.SetDoppelspielpartner(Guid.NewGuid());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder]);

        var command = new TriggerDoppelspielpartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node");

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<TriggerDoppelspielpartnerQueueing>();
        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();

        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielpartnerQueueing_AklStation_RespondsButDoesNotQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        hostOrder.RegisterForDoppelspiel();
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder]);

        var command = new TriggerDoppelspielpartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node");

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<TriggerDoppelspielpartnerQueueing>();
        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();

        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielpartnerQueueing_PartnerOrderHeadsTowardsFreeAklStation_LoopSwitch_RespondPublishesQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        nextPickOrder.IsQueued = false;
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithNextPickOrder(nextPickOrder)
            .WithinAklAgvLimit(true);

        await fixture.PublishMessageAsync(new TriggerDoppelspielpartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node"));
        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();

        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBe(nextPickOrder.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        nextPickOrder.IsQueued.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielpartnerQueueing_PartnerOrderHeadsTowardsFreeAklStation_RespondPublishesQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        nextPickOrder.IsQueued = false;
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithNextPickOrder(nextPickOrder)
            .WithinAklAgvLimit(true);

        var command = new TriggerDoppelspielpartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node");

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<TriggerDoppelspielpartnerQueueing>();
        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();

        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBe(nextPickOrder.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        nextPickOrder.IsQueued.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielpartnerQueueing_PartnerOrderHeadsTowardsOccupiedAklStation_RespondsButDoesNotQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        nextPickOrder.IsQueued = false;
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithNextPickOrder(nextPickOrder)
            .WithinAklAgvLimit(false);

        var command = new TriggerDoppelspielpartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node");

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<TriggerDoppelspielpartnerQueueing>();
        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();

        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielpartnerQueueing_NoNextPick_RespondsDoppelspielPartnerQueued()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        hostOrder.RegisterForDoppelspiel();
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithUnMatchedPickOrder(null);

        var command = new TriggerDoppelspielpartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node");

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<TriggerDoppelspielpartnerQueueing>();
        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();

        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielpartnerQueueing_NextPickAvailable_RespondsDoppelspielPartnerQueued_PublishesQueueHostOrder()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        hostOrder.RegisterForDoppelspiel();
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithNextPickOrder(nextPickOrder);

        var command = new TriggerDoppelspielpartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node");

        await fixture.TestHarness.Bus.Publish(command);

        await fixture.ShouldHaveConsumedAsync<TriggerDoppelspielpartnerQueueing>();
        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();

        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBe(nextPickOrder.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AssignedAgv);
    }
}