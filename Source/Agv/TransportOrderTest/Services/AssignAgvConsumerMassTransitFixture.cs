using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using Moq;
using System.Globalization;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.Common.Services.Routing;
using Gebhardt.StoreWare.Agv.TransportOrder.Services.Routing;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    internal class AssignAgvConsumerMassTransitFixture : MassTransitTestFixture<AssignAgvConsumer>, IAssignAgvConsumerTestFixtureBuilder
    {
        public static readonly string[] Orders = { "ORDER001", "ORDER002", "ORDER003" };
        public static readonly string[] Sources = { "SRC1", "SRC2", "SRC3" };
        public static readonly string[] Destinations = { "DST1", "DST2", "DST3" };
        public static readonly string[] LeNo = { "111", "222", "333" };
        public static readonly string[] AgvIds = { "AGV001", "AGV002" };
        private static readonly string[] _agvStates = { "Idle", "Busy" };
        private static readonly string[] _ips = { "127.0.0.1", "127.0.0.2" };
        private static readonly string[] _states = { nameof(OrderStateMachine.OrderReceived), nameof(OrderStateMachine.TransportOrderSend), nameof(OrderStateMachine.OrderReceived) };
        private static readonly DateTime[] _creationDates = { DateTime.ParseExact("2022-07-13 13:00:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTime.ParseExact("2022-07-13 12:00:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTime.ParseExact("2022-07-13 11:00:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) };

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

            _mockAgvRegistry = GetAgvRegistry();
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
            Build(_busRegistrationConfigurator, null, ConfigureServices);
            return this;
        }

        private void ConfigureServices(IServiceCollection s)
        {
            s.AddTransient(_ => _mockAgvRegistry.Object);
            s.AddTransient(_ => _mockOrderRegistry.Object);
            s.AddSingleton<IRoutingPlausibilityChecker, RoutingPlausibilityChecker>();

            foreach (Action<IServiceCollection> serviceAction in _serviceActions)
            {
                serviceAction(s);
            }
        }

        private Mock<IAgvRegistry> GetAgvRegistry()
        {
            var registry = new Mock<IAgvRegistry>();

            var agvs = new List<IAgv>();
            const string mapName = "Map#1";

            // Arrange
            string node01 = Sources[0];
            string node02 = Sources[1];
            string node03 = Destinations[0];
            string node04 = Destinations[1];
            string node05 = Sources[2];
            string node06 = Destinations[2];
            var agvGraph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node(node01){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f},
                    new AgvGraph.Node(node02){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 0f, Z = 0f},
                    new AgvGraph.Node(node03){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 3f, Z = 0f},
                    new AgvGraph.Node(node04){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 3f, Z = 0f},
                    new AgvGraph.Node(node05){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 6f, Y = 9f, Z = 0f},
                    new AgvGraph.Node(node06){ Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 13f, Y = 5f, Z = 0f}
                }, new[]
                {
                    new AgvGraph.Edge(node01, node02){ Metric = 4f},
                    new AgvGraph.Edge(node01, node03){ Metric = 3f},
                    new AgvGraph.Edge(node01, node04){ Metric = 5f},
                    new AgvGraph.Edge(node02, node01){ Metric = 4f},
                    new AgvGraph.Edge(node02, node04){ Metric = 6f},
                    new AgvGraph.Edge(node03, node01){ Metric = 3f},
                    new AgvGraph.Edge(node03, node04){ Metric = 4f},
                    new AgvGraph.Edge(node04, node01){ Metric = 5f},
                    new AgvGraph.Edge(node04, node02){ Metric = 6f},
                    new AgvGraph.Edge(node04, node03){ Metric = 4f}
                    //no edges for node 5 and 6 on purpose so that there is no route to this stations
                },
                mapName.ToGuid(),
                mapName);

            for (int i = 0; i < AgvIds.Length; i++)
            {
                string agvId = AgvIds[i];
                string order = Orders[i];
                string state = _agvStates[i];
                string ip = _ips[i];
                var correlationId = agvId.ToGuid();

                var agvStatus = new AgvStatus(correlationId, order, true, true, 10, true, true);
                var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());

                agvs.Add(new AgvSaga(correlationId, agvId, state, agvStatus, agvGraph, agvData) { Alive = true });
            }

            registry.SetupGet(x => x.Count).Returns(agvs.Count);
            registry.SetupGet(x => x.CountIdle).Returns(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle)).Count);
            registry.Setup(x => x.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle))));
            registry.Setup(x => x.GetAllAgvs()).Returns(new EnumerableQuery<IAgv>(agvs));
            registry.Setup(x => x.GetAgv(It.IsAny<Guid>())).Returns<Guid>(correlationId =>
            {
                foreach (var agv in agvs)
                {
                    if (agv.CorrelationId == correlationId)
                    {
                        return agv;
                    }
                }
                return null;
            });

            return registry;
        }

        public static Mock<IOrderRegistry> GetOrderRegistry(bool noCreationDates = false, bool empty = false)
        {
            var registry = new Mock<IOrderRegistry>();

            var orders = new List<IOrder>();
            if (!empty)
            {
                for (int i = 0; i < Orders.Length; i++)
                {
                    var order = new OrderDetails(Orders[i].ToGuid(), Orders[i], Sources[i], Destinations[i], LeNo[i], HostOrderCreator.Simulation, noCreationDates ? DateTime.Now : _creationDates[i]);
                    orders.Add(new OrderSaga(Orders[i].ToGuid(), order, _states[i], null));
                }
            }
            registry.Setup(x => x.GetAllOrders()).Returns(orders.AsQueryable);
            registry.Setup(x => x.GetUnassignedOrders()).Returns(orders.Where(x => x.CurrentState == nameof(OrderStateMachine.OrderReceived)).OrderBy(x => x.OrderDetails.CreationDate).AsQueryable);

            return registry;
        }

    }
}
