using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.CustomerConfiguration;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
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
            .WithHostCommunicator();

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        await fixture.PublishMessageAsync(new CreateHostOrder("Hu", orderDetails));

        fixture.VerifyReportOrderError("LeNo123", "SRC1", "DST1", CustomerConfiguration.ErrorCodeTargetUnknown, Times.Never());
        fixture.VerifyReportOrderError("LeNo123", "SRC1", "DST1", CustomerConfiguration.ErrorCodeSourceUnknown, Times.Never());
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

        await fixture.PublishMessageAsync(new CreateHostOrder(hu, orderDetails));

        await fixture.ShouldHavePublishedAsync<CancelOrAbortActionGroup>();
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

        var command = new FinishPick(order.CorrelationId,Guid.NewGuid(), DateTime.UtcNow);

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
        fixture.VerifyReportOrderError(order.Hu, order.Actions.First(a => a.ActionType == ActionType.Pick).Node, order.Actions.First(a => a.ActionType == ActionType.Drop).Node, CustomerConfiguration.ErrorCodeNoRoute, Times.Once());
    }
}