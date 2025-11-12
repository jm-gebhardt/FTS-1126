using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Messages.ActionGroups.Requests;
using Gebhardt.Acc.Common.Messages.HostOrder.Commands;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Gebhardt.Acc.PrismatCommunicationTest.Helper;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.PrismatCommunicationTest.Services;

[TestClass]
[DoNotParallelize]
public class HostOrderCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_CreateHostOrder_ValidData_CreatesOrder()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithDropOrderEligibleForDoppelSpiel(null)
            .WithinAklAgvLimit(true)
            .WithValidationError(null)
            .WithHostCommunicator();

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        await fixture.PublishMessageAsync(new CreateHostOrder("Hu", orderDetails));

        fixture.VerifyHostOrderAdded();
        fixture.VerifyReportOrderError("LeNo123", "SRC1", "DST1", KaeserGraphConfiguration.ErrorCodeTargetUnknown, Times.Never());
        fixture.VerifyReportOrderError("LeNo123", "SRC1", "DST1", KaeserGraphConfiguration.ErrorCodeSourceUnknown, Times.Never());
    }

    [DataRow(KaeserGraphConfiguration.ErrorCodeTargetUnknown)]
    [DataRow(KaeserGraphConfiguration.ErrorCodeSourceUnknown)]
    [TestMethod]
    public async Task Consume_CreateHostOrder_ValidationError_CreatesOrder(int errorCode)
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithValidationError((errorCode, "corresponding error info"))
            .WithHostCommunicator();

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        await fixture.PublishMessageAsync(new CreateHostOrder("Hu", orderDetails));

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
            .WithUnmatchedPickOrder(null)
            .WithHostOrderWithSameHu(unfinishedOrder)
            .WithHostCommunicator();

        await fixture.PublishMessageAsync(new CreateHostOrder(hu, orderDetails));

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
            .WithDropOrderEligibleForDoppelSpiel(null)
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
            .WithDropOrderEligibleForDoppelSpiel(null)
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
            .WithDropOrderEligibleForDoppelSpiel(null)
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
            .WithDropOrderEligibleForDoppelSpiel(Guid.NewGuid())
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
    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownPickTarget_NoDropOrdersEligibleForDoppelspiel_QueueActionGroup(int input)
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>(enteredAgvs: 1)
            .WithDropOrderEligibleForDoppelSpiel(null)
            .WithHostOrderRepository(null)
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownPickTargetOrder(pickTarget: input);

        var queueActionGroup = await fixture.PublishAndGetFirstMessageAsync<CreateHostOrder, QueueActionGroup>(new CreateHostOrder("Hu", order));
        fixture.VerifyHostOrderAdded(canRunDoppelspiel: false);

        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        var pickAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick);
        var dropAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Drop);
        var moveAction = queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Move);

        moveAction.Sequence.ShouldBe(0);
        pickAction.Sequence.ShouldBe(1);
        dropAction.Sequence.ShouldBe(2);

        moveAction.Target.Node.ShouldNotBeNull();
        moveAction.Target.Node.Name.ShouldBe(OrderGenerator.DecisionNode);
        pickAction.Target.Node.ShouldBeNull();
        dropAction.Target.Node.ShouldNotBeNull();
        dropAction.Target.Node.Name.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Destination]);
    }

    [DataRow(0)] //1020
    [DataRow(1)] //1040
    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownPickTarget_DropOrdersEligibleForDoppelspiel_PublishesCreateActionGroup(int input)
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>(enteredAgvs: 1)
            .WithDropOrderEligibleForDoppelSpiel(Guid.NewGuid())
            .WithHostOrders([dropOrder])
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownPickTargetOrder(pickTarget: input);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));
        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
        fixture.VerifyHostOrderAdded(canRunDoppelspiel: false);
        dropOrder.IsRegisteredForDoppelspielDrop.ShouldBeTrue();
    }

    [DataRow(null, true)]
    [DataRow(0, true)]
    [DataRow(1, false)]
    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownPickTarget_DropOrdersEligibleForDoppelspiel_QueueFillQuantityGiven_CanRunDoppelspielAccordingToFillQuantity(int? fillQuantity, bool canRunDoppelspiel)
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>(enteredAgvs: fillQuantity)
            .WithDropOrderEligibleForDoppelSpiel(Guid.NewGuid())
            .WithHostOrders([dropOrder])
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownPickTargetOrder(pickTarget: 0);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));
        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
        fixture.VerifyHostOrderAdded(canRunDoppelspiel: canRunDoppelspiel);
    }

    [DataRow(0)] //1050
    [DataRow(1)] //1060
    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownDropTarget_NoDropOrdersEligibleForDoppelspiel_QueueActionGroup(int output)
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrderEligibleForDoppelSpiel(null)
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

        pickAction.Target.Node.ShouldNotBeNull();
        pickAction.Target.Node.Name.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[order.Source]);
        moveAction.Target.Node.ShouldNotBeNull();
        moveAction.Target.Node.Name.ShouldBe(OrderGenerator.DecisionNode);
        dropAction.Target.Node.ShouldBeNull();
    }

    [DataRow(0)] //1050
    [DataRow(1)] //1060
    [TestMethod]
    public async Task Consume_CreateHostOrder_OrderWithUnknownDropTarget_DropOrdersEligibleForDoppelspiel_PublishesCreateActionGroup(int output)
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithDropOrderEligibleForDoppelSpiel(Guid.NewGuid())
            .WithHostOrders([dropOrder])
            .WithHostOrderWithSameHu(null)
            .WithValidationError(null)
            .WithHostCommunicator();

        var order = OrderGenerator.GenerateUnknownDropTargetOrder(dropTarget: output);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder("Hu", order));
        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        await fixture.ShouldHavePublishedAsync<CreateActionGroup>();
        fixture.VerifyHostOrderAdded();
        dropOrder.IsRegisteredForDoppelspielDrop.ShouldBeTrue();
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

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, DateTime.UtcNow));
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

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, now));
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
    public async Task Consume_TryQueueDoppelspielPartner_ShouldPickNext_ShortCircuit_PublishesQueueActionGroup()
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());

        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());
        dropOrder.RegisterDoppelspiel(ActionType.Drop);
        nextPickOrder.RegisterDoppelspiel(ActionType.Pick);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([dropOrder, nextPickOrder]);

        var command = new TryQueueDoppelspielPartner(Guid.NewGuid(), dropOrder.CorrelationId, nextPickOrder.CorrelationId, true, "pickNode".ToGraphNode());

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryQueueDoppelspielPartner>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Target.Node!.Name.ShouldBe(command.PickNode!.Name);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AgvCorrelationId);
        dropOrder.Drop?.DoppelspielPartner.ShouldBe(nextPickOrder.CorrelationId);
        nextPickOrder.Pick?.DoppelspielPartner.ShouldBe(dropOrder.CorrelationId);
    }

    [DataRow(0)] //1020
    [DataRow(1)] //1040
    [TestMethod]
    public async Task Consume_TryQueueDoppelspielPartner_ShouldPickNext_LongCircuitWE_PublishesQueueActionGroup(int input)
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());

        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder(pickTarget: input));
        dropOrder.RegisterDoppelspiel(ActionType.Drop);
        nextPickOrder.RegisterDoppelspiel(ActionType.Pick);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([dropOrder, nextPickOrder]);

        var command = new TryQueueDoppelspielPartner(Guid.NewGuid(), dropOrder.CorrelationId, nextPickOrder.CorrelationId, true, "pickNode".ToGraphNode());

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryQueueDoppelspielPartner>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick).Target.Node!.Name.ShouldBe(command.PickNode!.Name);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AgvCorrelationId);
        dropOrder.Drop?.DoppelspielPartner.ShouldBe(nextPickOrder.CorrelationId);
        nextPickOrder.Pick?.DoppelspielPartner.ShouldBe(dropOrder.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_TryQueueDoppelspielPartner_ShouldPickNext_LongCircuitAPL_PublishesQueueActionGroup()
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        dropOrder.RegisterDoppelspiel(ActionType.Drop);
        nextPickOrder.RegisterDoppelspiel(ActionType.Pick);
        string pickNode = nextPickOrder.Actions
            .Single(a => a.ActionType == ActionType.Pick).Node;
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([dropOrder, nextPickOrder]);

        var command = new TryQueueDoppelspielPartner(Guid.NewGuid(), dropOrder.CorrelationId, nextPickOrder.CorrelationId, true, pickNode.ToGraphNode());

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryQueueDoppelspielPartner>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        queueActionGroup.ActionGroup.Actions.Single(a => a.ActionType == ActionType.Pick)
            .Target.Node!.Name.ShouldBe(OrderGenerator.KaeserConfiguration.StationMapping[pickNode]);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AgvCorrelationId);
        dropOrder.Drop?.DoppelspielPartner.ShouldBe(nextPickOrder.CorrelationId);
        nextPickOrder.Pick?.DoppelspielPartner.ShouldBe(dropOrder.CorrelationId);
    }

    [TestMethod]
    public async Task Consume_TryQueueDoppelspielPartner_ShouldNotPickNext_UnmatchedDoppelspielPartner_PublishesQueueActionGroup()
    {
        var dropOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        dropOrder.RegisterDoppelspiel(ActionType.Drop);
        nextPickOrder.RegisterDoppelspiel(ActionType.Pick);
        string pickNode = nextPickOrder.Actions
            .Single(a => a.ActionType == ActionType.Pick).Node;
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([dropOrder])
            .WithUnmatchedPickOrder(nextPickOrder);

        var command = new TryQueueDoppelspielPartner(Guid.NewGuid(), dropOrder.CorrelationId, nextPickOrder.CorrelationId, false, pickNode.ToGraphNode());

        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<TryQueueDoppelspielPartner>();
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(3);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBeNull();
        dropOrder.Drop?.DoppelspielPartner.ShouldBeNull();
        nextPickOrder.Pick?.DoppelspielPartner.ShouldBeNull();
    }

    [TestMethod]
    public async Task Consume_AbortHostOrder_HasNotQueuedDoppelspielpartnerForDrop_PublishesQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        var doppelspielPartner = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.RegisterDoppelspiel(ActionType.Drop);
        doppelspielPartner.RegisterDoppelspiel(ActionType.Pick);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithUnmatchedPickOrder(doppelspielPartner)
            .WithinAklAgvLimit(true);

        var command = new AbortHostOrder(hostOrder.CorrelationId);
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        hostOrder.CanceledDate.ShouldNotBeNull();
        doppelspielPartner.Pick?.DoppelspielPartner.ShouldBeNull();
        doppelspielPartner.Pick?.IsRegisteredForDoppelspiel.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AbortHostOrder_HasNotQueuedDoppelspielpartnerForPick_UnregistersDoppelspielDrop()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());

        var doppelspielPartner = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.RegisterDoppelspiel(ActionType.Pick);
        doppelspielPartner.RegisterDoppelspiel(ActionType.Drop);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithUnmatchedDropOrder(doppelspielPartner)
            .WithinAklAgvLimit(true);

        var command = new AbortHostOrder(hostOrder.CorrelationId);
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
        hostOrder.CanceledDate.ShouldNotBeNull();
        doppelspielPartner.Pick?.DoppelspielPartner.ShouldBeNull();
        doppelspielPartner.Pick?.IsRegisteredForDoppelspiel.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Consume_AbortHostOrder_HasNotQueuedDoppelspielpartnerForDrop_StationIsOccupied_OrderLikelyReportedToConveyor_PublishesQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));

        var doppelspielPartner = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        hostOrder.RegisterDoppelspiel(ActionType.Drop);
        doppelspielPartner.RegisterDoppelspiel(ActionType.Pick);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithUnmatchedPickOrder(doppelspielPartner)
            .WithinAklAgvLimit(false);

        var command = new AbortHostOrder(hostOrder.CorrelationId);
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        hostOrder.CanceledDate.ShouldNotBeNull();
    }

    [TestMethod]
    public async Task Consume_AbortHostOrder_HasNotQueuedDoppelspielpartnerForDrop_StationIsOccupied_AklAgvLimitUnused_PublishesQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));

        var doppelspielPartner = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        hostOrder.RegisterDoppelspiel(ActionType.Drop);
        doppelspielPartner.RegisterDoppelspiel(ActionType.Pick);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithUnmatchedPickOrder(doppelspielPartner)
            .WithinAklAgvLimit(true);

        var command = new AbortHostOrder(hostOrder.CorrelationId);
        await fixture.PublishMessageAsync(command);

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
        await fixture.PublishMessageAsync(command);

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
        var order = OrderGenerator.GenerateHostOrderFromOrderDetails(new OrderDetails(orderId, "HU", "A", "B", "leNo", HostOrderCreator.Wms, DateTime.UtcNow));
        order.RegisterDoppelspiel(ActionType.Drop);
        order.FinishAction(DateTime.UtcNow, ActionType.Pick);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([order]);

        var command = new ResetDoppelspielPartner(orderId);
        await fixture.PublishMessageAsync(command);

        order.IsRegisteredForDoppelspielDrop.ShouldBeFalse();
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
    public async Task Consume_TriggerDoppelspielPartnerQueueing_HasAlreadyQueueDoppelspielPartner_RespondsDoppelspielPartnerQueued()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        hostOrder.RegisterDoppelspiel(ActionType.Drop);
        hostOrder.SetDoppelspielpartner(Guid.NewGuid(), ActionType.Drop);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder]);

        var command = new TriggerDoppelspielPartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node".ToGraphNode());
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();
        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielPartnerQueueing_AklStation_RespondsButDoesNotQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        hostOrder.RegisterDoppelspiel(ActionType.Drop);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder]);

        var command = new TriggerDoppelspielPartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node".ToGraphNode());
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();
        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielPartnerQueueing_PartnerOrderHeadsTowardsFreeAklStation_LoopSwitch_RespondPublishesQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        nextPickOrder.IsQueued = false;
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithNextPickOrder(nextPickOrder)
            .WithinAklAgvLimit(true);

        await fixture.PublishMessageAsync(new TriggerDoppelspielPartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node".ToGraphNode()));

        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();
        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBe(nextPickOrder.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        nextPickOrder.IsQueued.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielPartnerQueueing_PartnerOrderHeadsTowardsFreeAklStation_RespondPublishesQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        nextPickOrder.IsQueued = false;
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithNextPickOrder(nextPickOrder)
            .WithinAklAgvLimit(true);

        var command = new TriggerDoppelspielPartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node".ToGraphNode());
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();
        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBe(nextPickOrder.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        nextPickOrder.IsQueued.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielPartnerQueueing_PartnerOrderHeadsTowardsOccupiedAklStation_RespondsButDoesNotQueueActionGroup()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: true));
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder(pickAt1020: false));
        nextPickOrder.IsQueued = false;
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithNextPickOrder(nextPickOrder)
            .WithinAklAgvLimit(false);

        var command = new TriggerDoppelspielPartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node".ToGraphNode());

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();
        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielPartnerQueueing_NoNextPick_RespondsDoppelspielPartnerQueued()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        hostOrder.RegisterDoppelspiel(ActionType.Drop);
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithUnmatchedPickOrder(null);

        var command = new TriggerDoppelspielPartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node".ToGraphNode());

        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();
        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielPartnerQueueing_NextPickAvailable_RespondsDoppelspielPartnerQueued_PublishesQueueHostOrder()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        hostOrder.RegisterDoppelspiel(ActionType.Drop);
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithNextPickOrder(nextPickOrder)
            .WithinAklAgvLimit(true);

        var command = new TriggerDoppelspielPartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node".ToGraphNode());
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();
        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBe(nextPickOrder.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.Count.ShouldBe(2);
        queueActionGroup.ActionGroup.AssignedAgv.ShouldBe(command.AssignedAgv);
    }

    [TestMethod]
    public async Task Consume_TriggerDoppelspielPartnerQueueing_NextPickAvailableButAklLimitExceeded_DoesNotPublishesQueueHostOrder()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.Start(DateTime.UtcNow, Guid.NewGuid());
        hostOrder.RegisterDoppelspiel(ActionType.Drop);
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder])
            .WithNextPickOrder(nextPickOrder)
            .WithinAklAgvLimit(false);

        var command = new TriggerDoppelspielPartnerQueueing(hostOrder.CorrelationId, hostOrder.AssignedAgv!.Value, "node".ToGraphNode());
        await fixture.PublishMessageAsync(command);

        await fixture.ShouldHavePublishedAsync<DoppelspielPartnerQueued>();
        fixture.TestHarness.Published.Select<DoppelspielPartnerQueued>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryAssignDelayedOrderToAgv_NoCurrentHostOrder_RespondsAssignedDelayedOrderWithNull()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithCurrentOrderForAgv(null);

        var request = new TryAssignDelayedOrderToAgv(Guid.NewGuid(), Guid.Empty, new GraphNode("station".ToGuid(), "station"));
        await fixture.PublishMessageAsync(request);

        await fixture.ShouldHavePublishedAsync<AssignedDelayedOrder>();
        fixture.TestHarness.Published.Select<AssignedDelayedOrder>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryAssignDelayedOrderToAgv_CurrentHostOrderNotDropAtStation_RespondsAssignedDelayedOrderWithNull()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithCurrentOrderForAgv(hostOrder);

        var request = new TryAssignDelayedOrderToAgv(Guid.NewGuid(), Guid.Empty, new GraphNode("station".ToGuid(), "station"));
        await fixture.PublishMessageAsync(request);

        await fixture.ShouldHavePublishedAsync<AssignedDelayedOrder>();
        fixture.TestHarness.Published.Select<AssignedDelayedOrder>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryAssignDelayedOrderToAgv_NoNextPick_RespondsAssignedDelayedOrderWithNull()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithCurrentOrderForAgv(hostOrder);

        var request = new TryAssignDelayedOrderToAgv(Guid.NewGuid(), Guid.Empty, new GraphNode("APL1".ToGuid(), "Apl1"));
        await fixture.PublishMessageAsync(request);

        await fixture.ShouldHavePublishedAsync<AssignedDelayedOrder>();
        fixture.TestHarness.Published.Select<AssignedDelayedOrder>().First().Context.Message.CorrelationId.ShouldBeNull();
        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_TryAssignDelayedOrderToAgv_NextPick_RespondsAssignedDelayedOrder()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        var nextPick = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithCurrentOrderForAgv(hostOrder)
            .WithNextPickOrder(nextPick);

        var request = new TryAssignDelayedOrderToAgv(Guid.NewGuid(), Guid.Empty, new GraphNode("APL1".ToGuid(), "APL1"));
        await fixture.PublishMessageAsync(request);

        await fixture.ShouldHavePublishedAsync<AssignedDelayedOrder>();
        fixture.TestHarness.Published.Select<AssignedDelayedOrder>().First().Context.Message.CorrelationId.ShouldBe(nextPick.CorrelationId);
        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_ConsiderForDoppelspiel_ValidData_HostOrderCanRunDoppelspiel()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.CanRunDoppelspiel.ShouldBeFalse();

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder]);

        await fixture.PublishMessageAsync(new ConsiderForDoppelspiel(hostOrder.CorrelationId));

        hostOrder.CanRunDoppelspiel.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_QueueUnmatchedDoppelspielPicks_HasNextUnmatchedPick_PublishesQueueHostOrder()
    {
        var nextPickOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownPickTargetOrder());
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithUnmatchedPickOrder(nextPickOrder);

        var command = new QueueUnmatchedDoppelspielPicks(OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First());
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<QueueUnmatchedDoppelspielPicks>();

        await fixture.ShouldHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_QueueUnmatchedDoppelspielPicks_NoNextUnmatchedPick_DoesNotPublishQueueHostOrder()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithUnmatchedPickOrder(null);

        var command = new QueueUnmatchedDoppelspielPicks(OrderGenerator.KaeserConfiguration.ConveyorStationMapping.Keys.First());
        await fixture.TestHarness.Bus.Publish(command);
        await fixture.ShouldHaveConsumedAsync<QueueUnmatchedDoppelspielPicks>();

        await fixture.ShouldNotHavePublishedAsync<QueueActionGroup>();
    }

    [TestMethod]
    public async Task Consume_RetractHostOrder_ShortLoopOrder_RespondsFailed()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateShortCircuitOrder());

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder]);

        await fixture.PublishMessageAsync(new RetractHostOrder(hostOrder.CorrelationId));

        await fixture.ShouldHavePublishedAsync<Failed>();
    }

    [TestMethod]
    public async Task Consume_RetractHostOrder_UnknownHostOrder_DoesNotRespond()
    {
        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([]);

        await fixture.PublishMessageAsync(new RetractHostOrder(Guid.NewGuid()));

        await fixture.ShouldNotHavePublishedAsync<Failed>();
        await fixture.ShouldNotHavePublishedAsync<Success>();
    }

    [TestMethod]
    public async Task Consume_RetractHostOrder_DoppelspielOrder_RespondsFailed()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.IsQueued = true;
        hostOrder.RegisterDoppelspiel(ActionType.Pick);
        hostOrder.SetDoppelspielpartner(Guid.NewGuid(), ActionType.Pick);

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder]);

        await fixture.PublishMessageAsync(new RetractHostOrder(hostOrder.CorrelationId));

        await fixture.ShouldHavePublishedAsync<Failed>();
        hostOrder.IsQueued.ShouldBeTrue();
    }

    [TestMethod]
    public async Task Consume_RetractHostOrder_ValidData_RespondsSuccess()
    {
        var hostOrder = OrderGenerator.GenerateHostOrderFromOrderDetails(OrderGenerator.GenerateUnknownDropTargetOrder());
        hostOrder.IsQueued = true;

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrders([hostOrder]);

        await fixture.PublishMessageAsync(new RetractHostOrder(hostOrder.CorrelationId));

        await fixture.ShouldHavePublishedAsync<Success>();
        hostOrder.IsQueued.ShouldBeFalse();
        hostOrder.Actions.First(a => a.ActionType == ActionType.Pick).IsRegisteredForDoppelspiel.ShouldBeTrue();
    }
}