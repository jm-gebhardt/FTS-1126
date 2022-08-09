using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Controllers.Pagination;
using Gebhardt.StoreWare.Agv.TransportOrder.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Web.Controllers
{
    /// <summary>
    /// Tests TransportOrderApiController
    /// </summary>
    [TestClass]
    public class TransportOrderControllerTest
    {
        private static readonly string[] _orders = { "ORDER001", "ORDER002" };
        private static readonly string[] _sources = { "S1", "S2" };
        private static readonly string[] _destinations = { "D1", "D2" };
        private static readonly string[] _leNo = { "111", "222" };

        /// <summary>
        /// Test retrieving order list from saga repository
        /// </summary>
        [TestMethod]
        public async Task GetOrders_Returns_Ok()
        {
            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetIdleAgvs()).Returns(new List<IAgv>());

            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>(services: s => { s.AddTransient(_ => mockRegistry.Object); });
            
            var controller = new TransportOrderController(GetOrderRegistry());
            controller.ControllerContext = new ControllerContext(
                new ActionContext(new DefaultHttpContext(), 
                    new RouteData(), 
                    new ControllerActionDescriptor())
                );

            var inputOrderDetails = new OrderDetails(_orders[0].ToGuid(), _orders[0], _sources[0], _destinations[0], _leNo[0], HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(_orders[0].ToGuid(), inputOrderDetails));

            Assert.IsTrue(await fixture.SagaHarness.Consumed.Any<HandleHostOrder>());

            ActionResult<IEnumerable<Order>> actionResult = controller.GetOrders(new PaginationParameters());
            var result = actionResult.Result  as OkObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.IsInstanceOfType(result.Value, typeof(IEnumerable<Order>));
            
            List<Order> orders = (result.Value as IEnumerable<Order>)!.ToList();
            
            Assert.IsNotNull(orders);
            Assert.AreEqual(2, orders.Count);

            var resultOrder = orders.Last();
            Assert.AreEqual(resultOrder.CorrelationId, inputOrderDetails.CorrelationId);
            Assert.AreEqual(resultOrder.OrderDetails.OrderId, inputOrderDetails.OrderId);
            Assert.AreEqual(resultOrder.OrderDetails.Source, inputOrderDetails.Source);
            Assert.AreEqual(resultOrder.OrderDetails.Destination, inputOrderDetails.Destination);
            Assert.AreEqual(resultOrder.OrderDetails.LeNo, inputOrderDetails.LeNo);
            Assert.AreEqual(resultOrder.OrderDetails.Creator, inputOrderDetails.Creator);
        }

        private IOrderRegistry GetOrderRegistry()
        {
            var registry = new Mock<IOrderRegistry>();

            var orders = new List<IOrder>();

            for (int i = 0; i < _orders.Length; i++)
            {
                var order = new OrderDetails(_orders[i].ToGuid(), _orders[i], _sources[i], _destinations[i], _leNo[i], HostOrderCreator.Simulation);
                orders.Add(new OrderSaga(_orders[i].ToGuid(), order));
            }
            
            registry.Setup(x => x.GetAllOrders()).Returns(orders.AsQueryable());
            registry.Setup(x => x.GetUnassignedOrders()).Returns(orders.Where(x=> x.CurrentState == nameof(OrderStateMachine.OrderReceived)).AsQueryable());

            return registry.Object;
        }
    }
}
