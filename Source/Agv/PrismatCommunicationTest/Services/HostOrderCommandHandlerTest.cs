using Gebhardt.Acc.Common.Messages;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.HostOrder;
using Gebhardt.Acc.PrismatCommunication.Messages.Commands;
using Gebhardt.Acc.PrismatCommunication.Services.CommandHandler;
using Gebhardt.Acc.PrismatCommunicationTest.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

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
    public async Task Consume_FinishHostOrder_ValidData_ReportOrderCompletion()
    {
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId", "Hu");
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
        var hostOrder = new HostOrder(Guid.NewGuid(), "OrderId","Hu");
        hostOrder.Finish(now.AddDays(1));

        using HostOrderConsumerTestFixture<HostOrderCommandHandler> fixture = new HostOrderConsumerTestFixture<HostOrderCommandHandler>()
            .WithHostOrderRepository(hostOrder)
            .WithHostCommunicator();

        await fixture.TestHarness.Bus.Publish(new FinishHostOrder(hostOrder.CorrelationId, now));
        await fixture.ShouldHaveConsumedAsync<FinishHostOrder>();
        fixture.VerifyReportOrderCompletion(Times.Never());
    }
}