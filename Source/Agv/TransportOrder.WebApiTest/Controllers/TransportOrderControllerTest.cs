using AutoMapper;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Profiles;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.DateRange;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers.Pagination;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    [TestClass]
    public class TransportOrderControllerTest
    {
        #region Setup
        private static readonly string[] _orders = { "ORDER001", "ORDER002" };
        private static readonly string[] _states = { nameof(OrderStateMachine.OrderReceived), nameof(OrderStateMachine.Final) };
        private static readonly string[] _sources = { "S1", "S2" };
        private static readonly string[] _destinations = { "D1", "D2" };
        private static readonly string[] _leNo = { "111", "222" };

        private TransportOrderController GetController(IOrderRegistry? orderRegistry = null, IBus? bus = null)
        {
            orderRegistry ??= GetOrderRegistry();
            bus ??= new Mock<IBus>().Object;

            var controller = new TransportOrderController(orderRegistry, bus, GetMapper());
            controller.ControllerContext = new ControllerContext(
                new ActionContext(
                    new DefaultHttpContext(),
                    new RouteData(),
                    new ControllerActionDescriptor())
            );

            return controller;
        }

        private Mapper GetMapper()
        {
            var mapperConfig = new MapperConfiguration(cfg => { cfg.AddProfile(new MappingProfile()); });
            var mapper = new Mapper(mapperConfig);
            return mapper;
        }

        private IOrderRegistry GetOrderRegistry()
        {
            var registry = new Mock<IOrderRegistry>();
            var orders = new List<IOrder>();

            for (int i = 0; i < _orders.Length; i++)
            {
                var order = new OrderDetails(_orders[i].ToGuid(), _orders[i], _sources[i], _destinations[i], _leNo[i], HostOrderCreator.Simulation);
                orders.Add(new OrderSaga(_orders[i].ToGuid(), order, _states[i], null));
            }

            registry.Setup(x => x.GetAllOrders()).Returns(orders.AsQueryable());
            registry.Setup(x => x.GetUnassignedOrders()).Returns(orders.Where(x => x.CurrentState == nameof(OrderStateMachine.OrderReceived)).AsQueryable());
            registry.Setup(x => x.GetOrder(It.IsAny<Guid>())).Returns<Guid>(correlationId => orders.SingleOrDefault(a => a.CorrelationId == correlationId));

            return registry.Object;
        }
        #endregion

        /// <summary>
        /// Test retrieving order list from saga repository
        /// </summary>
        [TestMethod]
        public async Task GetOrders_Returns_Ok_AllOrdersReturned()
        {
            var controller = GetController();

            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>()));
            var mockObserver = new Mock<IStateObserver<OrderSaga>>();
            var orderEventObserver = new Mock<IEventObserver<OrderSaga>>();

            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>(services: s =>
            {
                s.AddTransient(_ => mockRegistry.Object);
                s.AddSingleton(_ => mockObserver.Object);
                s.AddSingleton(_ => orderEventObserver.Object);
            });

            var inputOrderDetails = new OrderDetails(_orders[0].ToGuid(), _orders[0], _sources[0], _destinations[0], _leNo[0], HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(_orders[0].ToGuid(), inputOrderDetails));
            (await fixture.SagaHarness.Consumed.Any<HandleHostOrder>()).ShouldBeTrue();

            ActionResult<IEnumerable<OrderDto>> actionResult = controller.GetOrders(new PaginationParameters(), new DateRangeParameters());
            var result = actionResult.Result as OkObjectResult;
            result.ShouldNotBeNull();
            result.StatusCode.ShouldBe(200);
            
            result.Value.ShouldBeAssignableTo(typeof(IEnumerable<OrderDto>));
            
            List<OrderDto> orders = (result.Value as IEnumerable<OrderDto>)!.ToList();
            orders.ShouldNotBeNull();
            orders.Count.ShouldBe(2);

            var resultOrder = orders.Last();
            inputOrderDetails.CorrelationId.ShouldBe(resultOrder.CorrelationId);
            inputOrderDetails.OrderId.ShouldBe(resultOrder.OrderDetails.OrderId);
            inputOrderDetails.Source.ShouldBe(resultOrder.OrderDetails.Source);
            inputOrderDetails.Destination.ShouldBe(resultOrder.OrderDetails.Destination);
            inputOrderDetails.LeNo.ShouldBe(resultOrder.OrderDetails.LeNo);
            inputOrderDetails.Creator.ShouldBe(resultOrder.OrderDetails.Creator);
        }

        [TestMethod]
        public void RequestWithCorrelationId_NoPagination_NoDateRange_OrderReturned()
        {
            var controller = GetController();

            ActionResult<IEnumerable<OrderDto>> result = controller.GetOrders(
                new PaginationParameters(),
                new DateRangeParameters()
            );
            result.Result.ShouldNotBeNull();

            var okResult = result.Result as OkObjectResult;
            okResult.ShouldNotBeNull();

            OrderDto[] orders = (okResult.Value as IEnumerable<OrderDto>)!.ToArray();
            orders.ShouldNotBeNull();
            orders.Length.ShouldBe(2);
        }

        [TestMethod]
        public void Request_RegistryEmpty_ReturnsEmptyList()
        {
            var orderRegistry = new Mock<IOrderRegistry>();
            orderRegistry.Setup(x => x.GetAllOrders()).Returns(Array.Empty<IOrder>().AsQueryable());
            orderRegistry.Setup(x => x.GetUnassignedOrders()).Returns(Array.Empty<IOrder>().AsQueryable());

            var controller = GetController(orderRegistry.Object);

            ActionResult<IEnumerable<OrderDto>> result = controller.GetOrders(
                new PaginationParameters(),
                new DateRangeParameters()
            );
            result.Result.ShouldNotBeNull();

            var okResult = result.Result as OkObjectResult;
            okResult.ShouldNotBeNull();

            OrderDto[] orders = (okResult.Value as IEnumerable<OrderDto>)!.ToArray();
            orders.ShouldNotBeNull();
            orders.Length.ShouldBe(0);
        }

        [TestMethod]
        public void ResetOrder_NotFound()
        {
            var controller = GetController();
            var correlationId = Guid.Empty;

            var result = controller.ResetOrder(correlationId).Result as NotFoundResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        public void ResetOrder_BadRequest()
        {
            var controller = GetController();
            var correlationId = _orders[0].ToGuid(); // Order not in state 'OrderFailed' or 'Final'

            var result = controller.ResetOrder(correlationId).Result as BadRequestObjectResult;
            result.ShouldNotBeNull();
        }

        [TestMethod]
        public void ResetOrder_Ok()
        {
            var bus = new Mock<IBus>();

            var controller = GetController(bus: bus.Object);
            var correlationId = _orders[1].ToGuid();

            var result = controller.ResetOrder(correlationId).Result as OkResult;
            result.ShouldNotBeNull();

            bus.Verify(s => s.Publish(It.Is<ResetTransportOrder>(
                m => m.CorrelationId == correlationId
            ), default), Times.Once);
        }

        [TestMethod]
        public void CompleteOrder_Ok()
        {
            var bus = new Mock<IBus>();

            var controller = GetController(bus: bus.Object);
            var correlationId = _orders[1].ToGuid();

            var result = controller.CompleteOrder(correlationId).Result as OkResult;
            result.ShouldNotBeNull();

            bus.Verify(s => s.Publish(It.Is<ManuallyCompleteTransportOrder>(
                m => m.CorrelationId == correlationId
            ), default), Times.Once);
        }

        [TestMethod]
        public void CompleteOrder_NotFound()
        {
            var controller = GetController();
            var correlationId = Guid.Empty;

            var result = controller.CompleteOrder(correlationId).Result as NotFoundResult;
            result.ShouldNotBeNull();
        }
    }
}