using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Commands;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Dtos;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Exceptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;
using Gebhardt.StoreWare.Agv.CommonTest.Helpers;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Handlers.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Handlers.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.DateRange;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries.Filtering.Pagination;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    [TestClass]
    public class TransportOrderControllerTest
    {
        /// <summary>
        /// Test retrieving order list from saga repository
        /// </summary>
        [TestMethod]
        public async Task GetOrders_Returns_Ok_AllOrdersReturned()
        {
            TransportOrderControllerTestFixture<GetOrdersRequest, GetOrdersRequestHandler> getOrdersFixture = new TransportOrderControllerTestFixture<GetOrdersRequest, GetOrdersRequestHandler>()
               .UseDefaultServices()
               .Build();
            var controller = getOrdersFixture.Controller;
            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetReadyAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>()));
            var mockObserver = new Mock<IStateObserver<OrderSaga>>();
            var orderEventObserver = new Mock<IEventObserver<OrderSaga>>();

            var fixture = new StateMachineTestFixture<OrderStateMachine, OrderSaga>(services: s =>
            {
                s.AddTransient(_ => mockRegistry.Object);
                s.AddSingleton(_ => mockObserver.Object);
                s.AddSingleton(_ => orderEventObserver.Object);
            });

            var inputOrderDetails = new OrderDetails(getOrdersFixture.Orders[0].ToGuid(), getOrdersFixture.Orders[0], getOrdersFixture.Sources[0], getOrdersFixture.Destinations[0], getOrdersFixture.LeNo[0], HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new HandleHostOrder(getOrdersFixture.Orders[0].ToGuid(), inputOrderDetails));
            (await fixture.SagaHarness.Consumed.Any<HandleHostOrder>()).ShouldBeTrue();

            ActionResult<IEnumerable<OrderDto>> actionResult = await controller.GetOrders(new PaginationParameters(), new DateRangeParameters());
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
        public async Task RequestWithCorrelationId_NoPagination_NoDateRange_OrderReturned()
        {
            TransportOrderControllerTestFixture<GetOrdersRequest, GetOrdersRequestHandler> getOrdersFixture = new TransportOrderControllerTestFixture<GetOrdersRequest, GetOrdersRequestHandler>()
               .UseDefaultServices()
               .Build();
            var controller = getOrdersFixture.Controller;

            ActionResult<IEnumerable<OrderDto>> result = await controller.GetOrders(
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
        public async Task Request_RegistryEmpty_ReturnsEmptyList()
        {
            var orderRegistry = new Mock<IOrderRegistry>();
            orderRegistry.Setup(x => x.GetAllOrders()).Returns(Array.Empty<IOrder>().AsQueryable());
            orderRegistry.Setup(x => x.GetUnassignedOrders()).Returns(Array.Empty<IOrder>().AsQueryable());

            TransportOrderControllerTestFixture<GetOrdersRequest, GetOrdersRequestHandler> getOrdersFixture = new TransportOrderControllerTestFixture<GetOrdersRequest, GetOrdersRequestHandler>()
               .UseDefaultServices()
               .AddOrderRegistryMock(orderRegistry)
               .Build();
            var controller = getOrdersFixture.Controller;

            ActionResult<IEnumerable<OrderDto>> result = await controller.GetOrders(
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
        public async Task ResetOrder_NotFound()
        {
            TransportOrderControllerTestFixture<ResetOrderCommand, ResetOrderCommandHandler> fixture = new TransportOrderControllerTestFixture<ResetOrderCommand, ResetOrderCommandHandler>()
               .UseDefaultServices()
               .Build();
            var controller = fixture.Controller;
            var correlationId = Guid.Empty;

            await MediatorShould.ThrowRequestExceptionAsync<NotFoundException>(async () => await controller.ResetOrder(correlationId));
        }

        [TestMethod]
        public async Task ResetOrder_BadRequest()
        {
            TransportOrderControllerTestFixture<ResetOrderCommand, ResetOrderCommandHandler> fixture = new TransportOrderControllerTestFixture<ResetOrderCommand, ResetOrderCommandHandler>()
               .UseDefaultServices()
               .Build();
            var controller = fixture.Controller;
            var correlationId = fixture.Orders[0].ToGuid(); // Order not in state 'OrderFailed' or 'Final'

            await MediatorShould.ThrowRequestExceptionAsync<NotInCorrectStateException>(async () => await controller.ResetOrder(correlationId));
        }

        [TestMethod]
        public async Task ResetOrder_Ok()
        {
            TransportOrderControllerTestFixture<ResetOrderCommand, ResetOrderCommandHandler> fixture = new TransportOrderControllerTestFixture<ResetOrderCommand, ResetOrderCommandHandler>()
               .UseDefaultServices()
               .Build();
            var controller = fixture.Controller;
            var correlationId = fixture.Orders[1].ToGuid();

            var result = controller.ResetOrder(correlationId).Result as OkResult;
            result.ShouldNotBeNull();

            var harness = fixture.TestHarness!;
            (await harness.Consumed.Any<ResetOrderCommand>()).ShouldBeTrue();
            (await harness.Published.Any<ResetOrderCommand>()).ShouldBeTrue();
        }

        [TestMethod]
        public async Task CompleteOrder_Ok()
        {
            TransportOrderControllerTestFixture<CompleteOrderCommand, CompleteOrderCommandHandler> fixture = new TransportOrderControllerTestFixture<CompleteOrderCommand, CompleteOrderCommandHandler>()
               .UseDefaultServices()
               .Build();
            var controller = fixture.Controller;
            var correlationId = fixture.Orders[1].ToGuid();

            var result = controller.CompleteOrder(correlationId).Result as OkResult;
            result.ShouldNotBeNull();

            var harness = fixture.TestHarness!;
            (await harness.Consumed.Any<CompleteOrderCommand>()).ShouldBeTrue();
            (await harness.Published.Any<CompleteOrderCommand>()).ShouldBeTrue();
        }

        [TestMethod]
        public async Task CompleteOrder_NotFound()
        {
            TransportOrderControllerTestFixture<CompleteOrderCommand, CompleteOrderCommandHandler> fixture = new TransportOrderControllerTestFixture<CompleteOrderCommand, CompleteOrderCommandHandler>()
               .UseDefaultServices()
               .Build();
            var controller = fixture.Controller;
            var correlationId = Guid.Empty;

            await MediatorShould.ThrowRequestExceptionAsync<NotFoundException>(async () => await controller.CompleteOrder(correlationId));
        }
    }
}