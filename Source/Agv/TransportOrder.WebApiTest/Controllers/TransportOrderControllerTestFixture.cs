using MassTransit;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Moq;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Features.Orders.Requests.Queries;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Controllers;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApiTest.Controllers
{
    public class TransportOrderControllerTestFixture<TRequest, TRequestHandler> : ControllerTestFixture<TRequest, TRequestHandler, TransportOrderController>
        where TRequest : class
        where TRequestHandler : class, IConsumer
    {
        public string[] Orders { get; } = { "ORDER001", "ORDER002" };
        private string[] States { get; } = { nameof(OrderStateMachine.OrderReceived), nameof(OrderStateMachine.Final) };
        public string[] Sources { get; } = { "S1", "S2" };
        public string[] Destinations { get; } = { "D1", "D2" };
        public string[] LeNo { get; } = { "111", "222" };

        private Mock<IOrderRegistry> GetOrderRegistry()
        {
            var registry = new Mock<IOrderRegistry>();
            var orders = new List<IOrder>();

            for (int i = 0; i < Orders.Length; i++)
            {
                var order = new OrderDetails(Orders[i].ToGuid(), Orders[i], Sources[i], Destinations[i], LeNo[i], HostOrderCreator.Simulation);
                orders.Add(new OrderSaga(Orders[i].ToGuid(), order, States[i], null));
            }

            registry.Setup(x => x.GetAllOrders()).Returns(orders.AsQueryable());
            registry.Setup(x => x.GetUnassignedOrders()).Returns(orders.Where(x => x.CurrentState == nameof(OrderStateMachine.OrderReceived)).AsQueryable());
            registry.Setup(x => x.GetOrder(It.IsAny<Guid>())).Returns<Guid>(correlationId => orders.SingleOrDefault(a => a.CorrelationId == correlationId));

            return registry;
        }

        /// <summary>
        /// Adds the service to the service collection
        /// </summary>
        /// <param name="serviceAction">The action of the service to add</param>
        /// <returns></returns>
        [UsedImplicitly]
        public TransportOrderControllerTestFixture<TRequest, TRequestHandler> AddService(Action<IServiceCollection> serviceAction)
        {
            ServiceActions.Add(serviceAction);
            return this;
        }

        /// <summary>
        /// Initializes the controller with the needed request client
        /// </summary>
        /// <returns></returns>
        protected override TransportOrderController GetController()
        {
            IRequestClient<ResetOrderCommand> resetOrderRequestClient = GetRequestClient<ResetOrderCommand>();
            IRequestClient<CompleteOrderCommand> completeOrderRequestClient = GetRequestClient<CompleteOrderCommand>();
            IRequestClient<GetOrdersRequest> getOrdersRequestClient = GetRequestClient<GetOrdersRequest>();

            var controller = new TransportOrderController(resetOrderRequestClient, completeOrderRequestClient, getOrdersRequestClient);
            return controller;
        }
        public TransportOrderControllerTestFixture<TRequest, TRequestHandler> UseDefaultServices()
        {

            ServiceActions.Add(sc => sc.AddTransient(_ => GetOrderRegistry().Object));
            ServiceActions.Add(sc => sc.AddTransient(_ => new Mock<IBus>().Object));
            ServiceActions.Add(sc => sc.AddTransient(_ => GetMapper()));
            return this;
        }

        /// <summary>
        /// Adds the <see cref="IOrderRegistry"/> mock object
        /// </summary>
        /// <returns></returns>
        [UsedImplicitly]
        public TransportOrderControllerTestFixture<TRequest, TRequestHandler> AddOrderRegistryMock(Action<Mock<IOrderRegistry>> orderRegistryAction)
        {
            var orderRegistryMock = new Mock<IOrderRegistry>();
            orderRegistryAction.Invoke(orderRegistryMock);

            ServiceActions.Add(sc => sc.AddTransient(_ => orderRegistryMock.Object));
            return this;
        }

        public TransportOrderControllerTestFixture<TRequest, TRequestHandler> AddOrderRegistryMock(Mock<IOrderRegistry> orderRegistry)
        {
            ServiceActions.Add(sc => sc.AddTransient(_ => orderRegistry.Object));
            return this;
        }

        public new TransportOrderControllerTestFixture<TRequest, TRequestHandler> Build()
        {
            base.Build();
            return this;
        }
    }
}