using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Hubs;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{

    [TestClass]
    public class TransportOrderChangedConsumerTest
    {
        [TestMethod]
        public async Task TransportOrderChanged_ConsumedAndMessageSentToClients()
        {
            var mockHub = new Mock<IOrderHubContext>();
            var fixture = new MassTransitTestFixture<TransportOrderChangedConsumer>(services =>
            {
                services.AddTransient(_ => mockHub.Object);
            });

            string orderId = "ORDER001";
            var correlationId = orderId.ToGuid();
            var agvCorrelationId = Guid.NewGuid();

            var orderDetails = new Common.Messages.OrderDetails(correlationId, orderId, "S01", "D01", "001", Common.Messages.HostOrderCreator.Simulation);
            IOrder order = new OrderDto("TransportOrderStarted", agvCorrelationId, orderDetails, correlationId);

            await fixture.TestHarness!.Bus.Publish(new TransportOrderChanged(correlationId, order));
            (await fixture.TestHarness.Published.Any<TransportOrderChanged>()).ShouldBeTrue();

            var consumedMessage = fixture.ConsumerHarness!.Consumed.Select<TransportOrderChanged>().First().Context.Message;
            consumedMessage.CorrelationId.ShouldBe(correlationId);
            consumedMessage.Order.OrderDetails.OrderId.ShouldBe(orderId);

            string method = nameof(TransportOrderChanged);
            mockHub.Verify(h => h.SendOrderToAllClients(method, It.Is<IOrder>(o => o.CorrelationId == correlationId), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
