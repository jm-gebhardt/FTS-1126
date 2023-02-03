using System.Globalization;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
{
    internal class AssignAgvConsumerMassTransitFixture : MassTransitTestFixture<AssignAgvConsumer>, IAssignAgvConsumerTestFixtureBuilder
    {
        public static string[] Orders => Services.AgvRegistryMock.Orders;
        public static string[] Sources => Services.AgvRegistryMock.Sources;
        public static string[] Destinations => Services.AgvRegistryMock.Destinations;

        public static readonly string[] LeNo = { "111", "222", "333" };
        private static readonly string[] _states = { nameof(OrderStateMachine.OrderReceived), nameof(OrderStateMachine.TransportOrderSent), nameof(OrderStateMachine.OrderReceived) };
        private static readonly DateTime[] _creationDates = {
            DateTime.ParseExact("2022-07-13 13:00:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTime.ParseExact("2022-07-13 12:00:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTime.ParseExact("2022-07-13 11:00:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
        };

        private readonly Mock<IAgvRegistry> _mockAgvRegistry;
        private Mock<IOrderRegistry> _mockOrderRegistry = null!;
        public static AgvGraph? Graph;
        private Action<IBusRegistrationConfigurator> _busRegistrationConfigurator = null!;
        private readonly List<Action<IServiceCollection>> _serviceActions = new();

        public AssignAgvConsumerMassTransitFixture()
        {
            const string mapName = "Map#1";
            Graph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node(Sources[0]) { Enabled = true, Type = AgvGraph.NodeType.Pick, X = 0f, Y = 0f, Z = 0f },
                    new AgvGraph.Node(Destinations[0]) { Enabled = true, Type = AgvGraph.NodeType.Drop, X = 1f, Y = 1f, Z = 0f }
                },
                new[]
                {
                    new AgvGraph.Edge(Sources[0], Destinations[0]) { Metric = 1f }
                },
                mapName.ToGuid(),
                mapName);

            _mockAgvRegistry = Services.AgvRegistryMock.GetAgvRegistry();
            SetOrderRegistry(GetOrderRegistry());

            Build();
        }

        public IAssignAgvConsumerTestFixtureBuilder SetOrderRegistry(Mock<IOrderRegistry> orderRegistry)
        {
            _mockOrderRegistry = orderRegistry;
            return this;
        }

        public IAssignAgvConsumerTestFixtureBuilder ConfigureServices(Action<IServiceCollection> services)
        {
            _serviceActions.Add(services);
            return this;
        }

        public IAssignAgvConsumerTestFixtureBuilder OrderMock(Action<Mock<IOrderRegistry>> orderRegistry)
        {
            orderRegistry(_mockOrderRegistry);
            return this;
        }

        public IAssignAgvConsumerTestFixtureBuilder AgvRegistryMock(Action<Mock<IAgvRegistry>> agvRegistry)
        {
            agvRegistry(_mockAgvRegistry);
            return this;
        }

        public IAssignAgvConsumerTestFixtureBuilder SetupBusRegistrationConfigurator(Action<IBusRegistrationConfigurator> busRegistrationConfigurator)
        {
            _busRegistrationConfigurator = busRegistrationConfigurator;
            return this;
        }

        public AssignAgvConsumerMassTransitFixture Build()
        {
            Build(_busRegistrationConfigurator, null);
            return this;
        }

        protected override void ConfigureServices(IServiceCollection s)
        {
            s.AddTransient(_ => _mockAgvRegistry.Object);
            s.AddTransient(_ => _mockOrderRegistry.Object);
            s.AddSingleton<IRoutingPlausibilityChecker, RoutingPlausibilityChecker>();

            foreach (Action<IServiceCollection> serviceAction in _serviceActions)
            {
                serviceAction(s);
            }
        }

        private static Mock<IOrderRegistry> GetOrderRegistry(bool noCreationDates = false, bool empty = false)
        {
            var registry = new Mock<IOrderRegistry>();

            var orders = new List<IOrder>();
            if (!empty)
            {
                for (int i = 0; i < Orders.Length; i++)
                {
                    var order = new OrderDetails(Orders[i].ToGuid(), Orders[i], Sources[i], Destinations[i], LeNo[i], HostOrderCreator.Simulation, noCreationDates ? DateTime.UtcNow : _creationDates[i]);
                    orders.Add(new OrderSaga(Orders[i].ToGuid(), order, _states[i], null));
                }
            }
            registry.Setup(x => x.GetAllOrders()).Returns(orders.AsQueryable);
            registry.Setup(x => x.GetUnassignedOrders()).Returns(orders.Where(x => x.CurrentState == nameof(OrderStateMachine.OrderReceived)).OrderBy(x => x.OrderDetails.CreationDate).AsQueryable);

            return registry;
        }

    }
}
