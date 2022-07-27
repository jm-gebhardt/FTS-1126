using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using MassTransit;
using MassTransit.Saga;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Utilities;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Web.Controllers
{
    /// <summary>
    /// Tests TransportOrderApiController
    /// </summary>
    [TestClass]
    public class TransportOrderControllerTest
    {
        /// <summary>
        /// Test retrieving order list from saga repository
        /// </summary>
        [TestMethod]
        public async Task GetOrdersTest()
        {
            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>();
            var repo = (SagaRepository<OrderSaga>)fixture.Provider.GetService(typeof(ISagaRepository<OrderSaga>));
            var controller = new TransportOrderController(repo);

            const string orderId = "Order#1";
            var correlationId = orderId.ToGuid();
            var inputOrder = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(correlationId, inputOrder));

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<HandleHostOrder>());

            var result = await controller.GetOrders() as OkObjectResult;
            Assert.IsNotNull(result);

            var orders = result.Value as List<Order>;
            Assert.IsNotNull(orders);
            Assert.AreEqual(1, orders.Count);

            var resultOrder = orders[0];
            Assert.AreEqual(resultOrder.CorrelationId, inputOrder.CorrelationId);
            Assert.AreEqual(resultOrder.OrderId, inputOrder.OrderId);
            Assert.AreEqual(resultOrder.Source, inputOrder.Source);
            Assert.AreEqual(resultOrder.Destination, inputOrder.Destination);
            Assert.AreEqual(resultOrder.LeNo, inputOrder.LeNo);
            Assert.AreEqual(resultOrder.Creator, inputOrder.Creator);
        }
    }
}
