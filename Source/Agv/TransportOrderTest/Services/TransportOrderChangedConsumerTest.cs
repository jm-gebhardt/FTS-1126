using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Hubs;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Models;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Test for finishing host orders
    /// </summary>
    [TestClass]
    public class TransportOrderChangedConsumerTest
    {
        /// <summary>
        /// Test handling a TransportOrderChanged event
        /// </summary>
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
            IOrder order = new Order("TransportOrderStarted", agvCorrelationId, orderDetails, correlationId);

            await fixture.TestHarness.Bus.Publish(new TransportOrderChanged(correlationId, order));
            Assert.IsTrue(await fixture.TestHarness.Published.Any<TransportOrderChanged>());

            var consumedMessage = fixture.ConsumerHarness.Consumed.Select<TransportOrderChanged>().First().Context.Message;
            Assert.AreEqual(correlationId, consumedMessage.CorrelationId);
            Assert.AreEqual(orderId, consumedMessage.Order.OrderDetails.OrderId);

            string method = nameof(TransportOrderChanged);
            mockHub.Verify(h => h.SendOrderToAllClients(method, It.Is<IOrder>(o => o.CorrelationId == correlationId), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
