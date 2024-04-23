using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Commands;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.WCSCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.WCSCommunication.Messages.Commands;
using Gebhardt.Acc.WCSCommunication.Services.CommandHandler;
using Gebhardt.Acc.WCSCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.WCSCommunicationTest.Services;

[TestClass]
public class HostOrderCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_CreateHostOrder_ValidData_CreatesOrderPublishesQueueActionGroupWithPickDropActions()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        fixture.HostOrderRepositoryMock.Setup(x => x.AddAsync(It.IsAny<HostOrder>())).Verifiable();

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        await fixture.TestHarness.Bus.Publish(new CreateHostOrder(orderDetails));

        (await fixture.ConsumerHarness.Consumed.Any<CreateHostOrder>()).ShouldBeTrue();
        fixture.HostOrderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<HostOrder>()), Times.Once);

        (await fixture.TestHarness.Published.Any<QueueActionGroup>()).ShouldBeTrue();
        var queueActionGroup = fixture.TestHarness.Published.Select<QueueActionGroup>().First().Context.Message;
        queueActionGroup.ActionGroup.Actions.First().ActionType.ShouldBe(ActionType.Pick);
        queueActionGroup.ActionGroup.Actions.Last().ActionType.ShouldBe(ActionType.Drop);
        queueActionGroup.CorrelationId.ShouldBe(correlationId);
    }

    [TestMethod]
    public async Task Consume_RemoveInvalidHostOrder_ValidData_TransmitHostOrderHandlingFailure()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);
        fixture.HostOrderRepositoryMock.Setup(x => x.RemoveAsync(It.IsAny<HostOrder>())).Verifiable();

        await fixture.TestHarness.Bus.Publish(new RemoveInvalidHostOrder(hostOrder.CorrelationId));
        (await fixture.ConsumerHarness.Consumed.Any<RemoveInvalidHostOrder>()).ShouldBeTrue();

        fixture.HostOrderRepositoryMock.Verify(x => x.RemoveAsync(It.IsAny<HostOrder>()), Times.Once);
        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderHandlingFailure(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_FinishHostOrder_ValidData_TransmitHostOrderHandlingSucceeded()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, DateTime.UtcNow));
        (await fixture.ConsumerHarness.Consumed.Any<FinishHostOrder>()).ShouldBeTrue();
        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderHandlingSucceeded(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_FinishHostOrder_InformationOutdated_NoTransmitHostOrderHandlingSucceeded()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var now = DateTime.UtcNow;
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        hostOrder.Finish(now.AddDays(1));
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, now));
        (await fixture.ConsumerHarness.Consumed.Any<FinishHostOrder>()).ShouldBeTrue();
        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderHandlingSucceeded(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_FinishHostOrderCancellation_ValidData_TransmitHostOrderCancellationSucceeded()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        await fixture.TestHarness.Bus.Publish(new FinishHostOrderCancellation(hostOrder.CorrelationId, DateTime.UtcNow));
        (await fixture.ConsumerHarness.Consumed.Any<FinishHostOrderCancellation>()).ShouldBeTrue();
        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderCancellationSucceeded(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_FinishHostOrderCancellation_AlreadyCanceled_NoTransmitHostOrderCancellationSucceeded()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var now = DateTime.UtcNow;
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        hostOrder.Cancel(now.AddDays(1));
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        await fixture.TestHarness.Bus.Publish(new FinishHostOrderCancellation(hostOrder.CorrelationId, now));
        (await fixture.ConsumerHarness.Consumed.Any<FinishHostOrderCancellation>()).ShouldBeTrue();
        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderCancellationSucceeded(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_FinishHostOrderCancellation_AlreadyFinished_NoTransmitHostOrderCancellationSucceeded()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var now = DateTime.UtcNow;
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        hostOrder.Finish(now.AddDays(1));
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        await fixture.TestHarness.Bus.Publish(new FinishHostOrderCancellation(hostOrder.CorrelationId, now));
        (await fixture.ConsumerHarness.Consumed.Any<FinishHostOrderCancellation>()).ShouldBeTrue();
        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderCancellationSucceeded(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_HandleHostOrderStart_ValidData_TransmitHostOrderStarted()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        await fixture.TestHarness.Bus.Publish(new HandleHostOrderStart(hostOrder.CorrelationId, "agv", DateTime.UtcNow));
        (await fixture.ConsumerHarness.Consumed.Any<HandleHostOrderStart>()).ShouldBeTrue();

        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderStarted(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_HandleHostOrderStart_InformationOutdated_NoTransmitHostOrderStarted()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        var now = DateTime.UtcNow;
        hostOrder.Start(now.AddDays(1));
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        await fixture.TestHarness.Bus.Publish(new HandleHostOrderStart(hostOrder.CorrelationId, "agv", now));
        (await fixture.ConsumerHarness.Consumed.Any<HandleHostOrderStart>()).ShouldBeTrue();

        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderStarted(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Consume_HandleCancellationFailure_ValidData_TransmitHostOrderCancellationFailure()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        await fixture.TestHarness.Bus.Publish(new HandleCancellationFailure(hostOrder.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyFinished));
        (await fixture.ConsumerHarness.Consumed.Any<HandleCancellationFailure>()).ShouldBeTrue();

        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderCancellationFailure(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_HandleHostOrderFailure_ValidData_TransmitHostOrderHandlingFailure()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        await fixture.TestHarness.Bus.Publish(new HandleHostOrderFailure(hostOrder.CorrelationId, HostOrderHandlingFailedErrorCode.FailedByTransportLayer, DateTime.UtcNow));
        (await fixture.ConsumerHarness.Consumed.Any<HandleHostOrderFailure>()).ShouldBeTrue();

        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderHandlingFailure(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task Consume_HandleHostOrderFailure_InformationOutdated_NoTransmitHostOrderHandlingFailure()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>();
        var now = DateTime.UtcNow;
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        hostOrder.HandleFailure(now.AddDays(1));
        fixture.HostOrderRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(hostOrder);

        await fixture.TestHarness.Bus.Publish(new HandleHostOrderFailure(hostOrder.CorrelationId, HostOrderHandlingFailedErrorCode.FailedByTransportLayer, now));
        (await fixture.ConsumerHarness.Consumed.Any<HandleHostOrderFailure>()).ShouldBeTrue();

        fixture.HostCommunicatorMock.Verify(x => x.TransmitHostOrderHandlingFailure(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}