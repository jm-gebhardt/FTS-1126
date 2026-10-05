using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.HostOrder.Events;
using Gebhardt.Acc.WCSCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.WCSCommunication.Messages.Commands;
using Gebhardt.Acc.WCSCommunication.Services.CommandHandler;
using Gebhardt.Acc.WCSCommunicationTest.Fixtures;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.WCSCommunicationTest.Services;

[TestClass]
public class HostOrderCommandHandlerTest
{
    [TestMethod]
    public async Task Consume_CreateHostOrder_ValidData_CreatesOrderPublishesHostOrderCreated()
    {
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(null)
            .WithHostCommunicator();

        const string orderId = "Order#01";
        var correlationId = orderId.ToGuid();
        var orderDetails = new OrderDetails(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

        var hostOrderCreated = await fixture.PublishAndGetFirstMessageAsync<CreateHostOrder, HostOrderCreated>(
            new CreateHostOrder(orderDetails));
        
        await fixture.ShouldHaveConsumedAsync<CreateHostOrder>();
        fixture.VerifyHostOrderAdded();
        
        await fixture.ShouldHavePublishedAsync<HostOrderCreated>();
        hostOrderCreated.Hu.ShouldBe(orderDetails.LeNo);
    }

    [TestMethod]
    public async Task Consume_RemoveInvalidHostOrder_ValidData_TransmitHostOrderHandlingFailure()
    {
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();
        await fixture.TestHarness.Bus.Publish(new RemoveInvalidHostOrder(hostOrder.CorrelationId));
        await fixture.ShouldHaveConsumedAsync<RemoveInvalidHostOrder>();

        fixture.VerifyHostOrderRemoved();
        fixture.VerifyHostOrderHandlingFailure();
    }

    [TestMethod]
    public async Task Consume_FinishHostOrder_ValidData_TransmitHostOrderHandlingSucceeded()
    {
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, DateTime.UtcNow));
        await fixture.ShouldHaveConsumedAsync<FinishHostOrder>();
        fixture.VerifyHostOrderHandlingSucceeded();
    }

    [TestMethod]
    public async Task Consume_FinishHostOrder_InformationOutdated_NoTransmitHostOrderHandlingSucceeded()
    {
        var now = DateTime.UtcNow;
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        hostOrder.Finish(now.AddDays(1));
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, now));
        await fixture.ShouldHaveConsumedAsync<FinishHostOrder>();
        fixture.VerifyHostOrderHandlingSucceeded(Times.Never());
    }

    [TestMethod]
    public async Task Consume_FinishHostOrderCancellation_ValidData_TransmitHostOrderCancellationSucceeded()
    {
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();
        
        await fixture.TestHarness.Bus.Publish(new FinishHostOrderCancellation(hostOrder.CorrelationId, DateTime.UtcNow));
        await fixture.ShouldHaveConsumedAsync<FinishHostOrderCancellation>();
        fixture.VerifyHostOrderCancellationSucceeded();
    }

    [TestMethod]
    public async Task Consume_FinishHostOrderCancellation_AlreadyCanceled_NoTransmitHostOrderCancellationSucceeded()
    {
        var now = DateTime.UtcNow;
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        hostOrder.Cancel(now.AddDays(1));
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new FinishHostOrderCancellation(hostOrder.CorrelationId, now));
        await fixture.ShouldHaveConsumedAsync<FinishHostOrderCancellation>();
        fixture.VerifyHostOrderCancellationSucceeded(Times.Never());
    }

    [TestMethod]
    public async Task Consume_FinishHostOrderCancellation_AlreadyFinished_NoTransmitHostOrderCancellationSucceeded()
    {
        var now = DateTime.UtcNow;
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        hostOrder.Finish(now.AddDays(1));
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new FinishHostOrderCancellation(hostOrder.CorrelationId, now));
        await fixture.ShouldHaveConsumedAsync<FinishHostOrderCancellation>();
        fixture.VerifyHostOrderCancellationSucceeded(Times.Never());
    }

    [TestMethod]
    public async Task Consume_HandleHostOrderStart_ValidData_TransmitHostOrderStarted()
    { 
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();
       
        await fixture.TestHarness.Bus.Publish(new HandleHostOrderStart(hostOrder.CorrelationId, "agv", DateTime.UtcNow));
        await fixture.ShouldHaveConsumedAsync<HandleHostOrderStart>();

        fixture.VerifyHostOrderStarted();
    }

    [TestMethod]
    public async Task Consume_HandleHostOrderStart_InformationOutdated_NoTransmitHostOrderStarted()
    {
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        var now = DateTime.UtcNow;
        hostOrder.Start(now.AddDays(1));
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();
       
        await fixture.TestHarness.Bus.Publish(new HandleHostOrderStart(hostOrder.CorrelationId, "agv", now));
        await fixture.ShouldHaveConsumedAsync<HandleHostOrderStart>();

        fixture.VerifyHostOrderStarted(Times.Never());
    }

    [TestMethod]
    public async Task Consume_HandleCancellationFailure_ValidData_TransmitHostOrderCancellationFailure()
    {
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new HandleCancellationFailure(hostOrder.CorrelationId, HostOrderCancelFailedErrorCode.OrderAlreadyFinished));
        await fixture.ShouldHaveConsumedAsync<HandleCancellationFailure>();

        fixture.VerifyHostOrderCancellationFailure();
    }

    [TestMethod]
    public async Task Consume_HandleHostOrderFailure_ValidData_TransmitHostOrderHandlingFailure()
    {
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new HandleHostOrderFailure(hostOrder.CorrelationId, HostOrderHandlingFailedErrorCode.FailedByTransportLayer, DateTime.UtcNow));
        await fixture.ShouldHaveConsumedAsync<HandleHostOrderFailure>();

        fixture.VerifyHostOrderHandlingFailure();
    }

    [TestMethod]
    public async Task Consume_HandleHostOrderFailure_InformationOutdated_NoTransmitHostOrderHandlingFailure()
    {
        var now = DateTime.UtcNow;
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId");
        hostOrder.HandleFailure(now.AddDays(1));
        
        using var fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new HandleHostOrderFailure(hostOrder.CorrelationId, HostOrderHandlingFailedErrorCode.FailedByTransportLayer, now));
        await fixture.ShouldHaveConsumedAsync<HandleHostOrderFailure>();

        fixture.VerifyHostOrderHandlingFailure(Times.Never());
    }
}