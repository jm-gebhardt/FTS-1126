using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using JetBrains.Annotations;
using MassTransit;
using Moq;
using AssignAgvConsumer = Gebhardt.StoreWare.Agv.TransportOrder.Services.AssignAgvConsumer;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    [UsedImplicitly]
    internal class AgvReservationSucceededConsumer : IConsumer<ReserveAgv>
    {
        public async Task Consume(ConsumeContext<ReserveAgv> context)
        {
            await context.RespondAsync(new AgvReservationSucceeded(context.Message.CorrelationId));
        }
    }

    [UsedImplicitly]
    internal class AgvReservationRejectedConsumer : IConsumer<ReserveAgv>
    {
        public async Task Consume(ConsumeContext<ReserveAgv> context)
        {
            await context.RespondAsync(new AgvReservationRejected(context.Message.CorrelationId));
        }
    }

    /// <summary>
    /// Test for handling Handle AssignAgv messages
    /// </summary>
    [TestClass]
    public class AssignAgvConsumerTest
    {
        private static readonly string[] _orders = { "ORDER001", "ORDER002", "ORDER003" };
        private static readonly string[] _sources = { "SRC1", "SRC2", "SRC3" };
        private static readonly string[] _destinations = { "DST1", "DST2", "DST3" };
        private static readonly string[] _leNo = { "111", "222", "333" };
        private static readonly string[] _agvIds = { "AGV001", "AGV002" };
        private static readonly string[] _agvStates = { "Idle", "Busy" };
        private static readonly string[] _ips = { "127.0.0.1", "127.0.0.2" };
        private static readonly string[] _states = { nameof(OrderStateMachine.OrderReceived), nameof(OrderStateMachine.TransportOrderSend), nameof(OrderStateMachine.OrderReceived) };
        private static readonly DateTime[] _creationDates = { DateTime.ParseExact("2022-07-13 13:00:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTime.ParseExact("2022-07-13 12:00:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTime.ParseExact("2022-07-13 11:00:01", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) };
        private static AgvGraph? _graph;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            const string mapName = "Map#1";
            _graph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node(_sources[0]) { Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 0f, Z = 0f },
                    new AgvGraph.Node(_destinations[0]) { Enabled = true, Type = AgvGraph.NodeType.Station, X = 1f, Y = 1f, Z = 0f }
                },
                new[]
                {
                   new AgvGraph.Edge(_sources[0], _destinations[0]) { Metric = 1f }
                },
                mapName.ToGuid(),
                mapName);
        }

        /// <summary>
        /// Test cleanup.
        /// </summary>
        [ClassCleanup]
        public static void ClassCleanup()
        {
        }

        [TestMethod]
        public async Task When_AgvRegistryIsEmpty_IAgvAssignFailed_isPublished()
        {
            var mockAgvRegistry = new Mock<IAgvRegistry>();
            Mock<IOrderRegistry> mockOrderRegistry = GetOrderRegistry();
            mockAgvRegistry.Setup(s => s.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>()));
            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(
                services: s =>
                {
                    s.AddTransient(_ => mockAgvRegistry.Object);
                    s.AddTransient(_ => mockOrderRegistry.Object);
                }
                );

            string orderId = "0001";
            var correlationId = orderId.ToGuid();
            var order = new OrderDetails(correlationId, orderId, _sources[0], _destinations[0], "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new AssignAgv(correlationId, order));

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvAssignmentSucceeded>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvAssignmentFailed>());
        }

        /// <summary>
        /// Receive AssignAgv message when no Idle AGV is available
        /// </summary>
        [TestMethod]
        public async Task When_NoIdleAgvIsIdle_AgvAssignFailedIsPublished()
        {
            const string agvId = "AGV001";
            var agvCorrelationId = agvId.ToGuid();
            var mockAgvRegistry = new Mock<IAgvRegistry>();
            Mock<IOrderRegistry> mockOrderRegistry = GetOrderRegistry();
            mockAgvRegistry.Setup(s => s.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>
            {
                new AgvSaga(agvCorrelationId, agvId, nameof(AgvStateMachine.Idle), agvGraph: _graph)
            }));

            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(
                services: s =>
                {
                    s.AddSingleton((_) => mockAgvRegistry.Object);
                    s.AddSingleton((_) => mockOrderRegistry.Object);
                },
                busRegistrationConfigurator: x =>
                {
                    x.AddRequestClient<ReserveAgv>();
                    x.AddConsumer<AgvReservationRejectedConsumer>();
                }
            );

            const string orderId = "0001";
            var correlationId = orderId.ToGuid();
            var order = new OrderDetails(correlationId, orderId, _sources[0], _destinations[0], "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new AssignAgv(correlationId, order));

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvAssignmentSucceeded>());

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvAssignmentFailed>());
        }

        /// <summary>
        /// Receive AssignAgv message and assign order to Idle AGV
        /// </summary>
        /// <param name="alive">Influence AGV alive state</param>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task When_IdleAgvIsAvailable_AgvAssignSucceededWhenAliveElseFailed(bool alive)
        {
            const string agvId = "AGV001";
            var agvCorrelationId = agvId.ToGuid();
            var mockAgvRegistry = new Mock<IAgvRegistry>();
            Mock<IOrderRegistry> mockOrderRegistry = GetOrderRegistry();
            mockAgvRegistry.Setup(s => s.GetIdleAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>
            {
                new AgvSaga(agvCorrelationId, agvId, nameof(AgvStateMachine.Idle), agvGraph: _graph) { Alive = alive }
            }));

            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(
                services: s =>
                {
                    s.AddSingleton((_) => mockAgvRegistry.Object);
                    s.AddSingleton((_) => mockOrderRegistry.Object);
                },
                busRegistrationConfigurator: x =>
                {
                    x.AddRequestClient<ReserveAgv>();
                    x.AddConsumer<AgvReservationSucceededConsumer>();
                }
            );

            var correlationId = _orders[0].ToGuid();
            var order = new OrderDetails(correlationId, _orders[0], _sources[0], _destinations[0], _leNo[0], HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new AssignAgv(correlationId, order));

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());

            if (alive)
            {
                // When there is an idle AGV with alive state we expect AgvAssignmentSucceeded
                var message = fixture.TestHarness.Published.Select<AgvAssignmentSucceeded>(s => s.Context.CorrelationId == correlationId).First().Context.Message;
                Assert.AreEqual(agvId, message.AgvId);
            }
            else
            {
                // When there is just one idle AGV (but offlint) we expect AgvAssignmentFailed
                var message = fixture.TestHarness.Published.Select<AgvAssignmentFailed>(s => s.Context.CorrelationId == correlationId).FirstOrDefault()?.Context.Message;
                Assert.IsNotNull(message);
            }
        }

        /// <summary>
        /// Checks IAssignAgv consumption and calculation with LastNodeId vs. alive state
        /// </summary>
        /// <param name="aliveAgv1">Influence alive state of AGV 1</param>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ConsumeIAssignAgv_DetermineAgvRegardingLastNodeIdAndAliveState(bool aliveAgv1)
        {
            // Arrange
            const string node01 = "node01";
            const string node02 = "node02";
            const string node03 = "node03";
            const string node04 = "node04";
            const string mapName = "Map#1";
            var agvGraph = new AgvGraph(Guid.Empty,
                new[]
            { /*   Map#1:  (AGV_01) 1   2
               *                       /
               *                      / (ORD_2-3, but this edge doesn't exist)
               *                     /
               *                    3   4 (AGV_02)   */
                new AgvGraph.Node(node01){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 0f, Z = 0f},
                new AgvGraph.Node(node02){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 4f, Y = 0f, Z = 0f},
                new AgvGraph.Node(node03){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 3f, Z = 0f},
                new AgvGraph.Node(node04){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 4f, Y = 3f, Z = 0f}
            }, new[]
            {
                new AgvGraph.Edge(node01, node02){ Metric = 4f},    // Used by AGV_01 to travel to source
                new AgvGraph.Edge(node01, node03){ Metric = 3f},
                new AgvGraph.Edge(node01, node04){ Metric = 5f},
                new AgvGraph.Edge(node02, node01){ Metric = 4f},
                new AgvGraph.Edge(node02, node04){ Metric = 6f},
                new AgvGraph.Edge(node03, node01){ Metric = 3f},
                new AgvGraph.Edge(node03, node04){ Metric = 4f},
                new AgvGraph.Edge(node04, node01){ Metric = 5f},
                new AgvGraph.Edge(node04, node02){ Metric = 6f},    // Used by AGV_02 to travel to source (more expensive)
                new AgvGraph.Edge(node04, node03){ Metric = 4f}
            },
                mapName.ToGuid(),
                mapName);
            const string agvId = "AGV_01";
            var closestAgvCorrelationId = agvId.ToGuid();
            // Current location is node01
            var agvStatus1 = new AgvStatus(closestAgvCorrelationId, null, false, true, 50, false, false, node01);

            const string agvId2 = "AGV_02";
            var agvCorrelationId2 = agvId2.ToGuid();
            // Current location ist node04
            var agvStatus2 = new AgvStatus(closestAgvCorrelationId, null, false, true, 50, false, false, node04);

            var mockAgvRegistry = new Mock<IAgvRegistry>();
            Mock<IOrderRegistry> mockOrderRegistry = GetOrderRegistry();
            mockAgvRegistry.Setup(mReg => mReg.GetIdleAgvs()).Returns(
                new EnumerableQuery<IAgv>(
                    new List<IAgv>
                    {
                        new AgvSaga(closestAgvCorrelationId, agvId, nameof(AgvStateMachine.Idle), agvGraph: agvGraph, agvStatus: agvStatus1) { Alive = aliveAgv1 },
                        new AgvSaga(agvCorrelationId2, agvId2, nameof(AgvStateMachine.Idle), agvGraph: agvGraph, agvStatus: agvStatus2) { Alive = true }
                    }
            ));

            const string orderId = "ORD_2->3";
            const string source = node02;
            const string destination = node03;
            const string leNo = "LeNo123";
            var order = new OrderDetails(
                orderId.ToGuid(),
                orderId,
                source,
                destination,
                leNo,
                HostOrderCreator.Simulation
            );

            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(
                s =>
                {
                    s.AddTransient(_ => mockAgvRegistry.Object);
                    s.AddTransient(_ => mockOrderRegistry.Object);
                },
                busRegistrationConfigurator: x =>
            {
                x.AddRequestClient<ReserveAgv>();
                x.AddConsumer<AgvReservationSucceededConsumer>();
            });

            // Act
            await fixture.TestHarness.Bus.Publish(new AssignAgv(order.CorrelationId, order));


            // Assert
            Assert.IsTrue(await fixture.TestHarness.Published.Any<AssignAgv>());

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());
            var consumedIAssignAgvMessage = fixture.TestHarness.Consumed.Select<AssignAgv>().First().Context.Message;
            Assert.AreEqual(order.CorrelationId, consumedIAssignAgvMessage.Order.CorrelationId);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvAssignmentSucceeded>());
            var response = fixture.TestHarness.Published.Select<AgvAssignmentSucceeded>().First().Context.Message;
            if (aliveAgv1)
            {
                Assert.AreEqual(closestAgvCorrelationId, response.AgvCorrelationId);
                CollectionAssert.AreEqual(new[] { agvStatus1.LastNodeId, node02, node01, node03 }, response.Nodes);
            }
            else
            {
                Assert.AreEqual(agvCorrelationId2, response.AgvCorrelationId);
                CollectionAssert.AreEqual(new[] { agvStatus2.LastNodeId, node02, node01, node03 }, response.Nodes);
            }
        }

        /// <summary>
        /// Agv became idle but there is no order to assign
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AgvBecameIdle_NoOrderToAssign()
        {

            Mock<IAgvRegistry> mockAgvRegistry = GetAgvRegistry();
            Mock<IOrderRegistry> mockOrderRegistry = GetOrderRegistry(false, true);

            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(s =>
                {
                    s.AddTransient(_ => mockAgvRegistry.Object);
                    s.AddTransient(_ => mockOrderRegistry.Object);
                }
            );

            await fixture.TestHarness.Bus.Publish(new AgvBecameIdle(_agvIds[0].ToGuid()));
            Assert.IsTrue(await fixture.TestHarness.Consumed.Any<AgvBecameIdle>());
            Assert.IsFalse(await fixture.TestHarness.Published.Any<RequestOrderAgvAssign>());
        }


        /// <summary>
        /// Agv became idle, check if oldest order is assigned
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AgvBecameIdle_Assign_Oldest_UnassignedOrder()
        {

            Mock<IAgvRegistry> mockAgvRegistry = GetAgvRegistry();
            Mock<IOrderRegistry> mockOrderRegistry = GetOrderRegistry(true);

            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(s =>
                {
                    s.AddTransient(_ => mockAgvRegistry.Object);
                    s.AddTransient(_ => mockOrderRegistry.Object);
                }
            );

            await fixture.TestHarness.Bus.Publish(new AgvBecameIdle(_agvIds[0].ToGuid()));
            Assert.IsTrue(await fixture.TestHarness.Consumed.Any<AgvBecameIdle>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestOrderAgvAssign>());
            var message = fixture.TestHarness.Published.Select<RequestOrderAgvAssign>().First().Context.Message;
            Assert.AreEqual(_orders[0].ToGuid(), message.CorrelationId);
        }

        /// <summary>
        /// Agv became idle, oldest order has no route so take next
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AgvBecameIdle_Assign_Oldest_UnassignedOrder_ThatHasRoute()
        {
            Mock<IAgvRegistry> mockAgvRegistry = GetAgvRegistry();
            Mock<IOrderRegistry> mockOrderRegistry = GetOrderRegistry();

            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(s =>
                {
                    s.AddTransient(_ => mockAgvRegistry.Object);
                    s.AddTransient(_ => mockOrderRegistry.Object);
                }
            );

            await fixture.TestHarness.Bus.Publish(new AgvBecameIdle(_agvIds[0].ToGuid()));
            Assert.IsTrue(await fixture.TestHarness.Consumed.Any<AgvBecameIdle>());
            Assert.IsTrue(await fixture.TestHarness.Published.Any<RequestOrderAgvAssign>());
            var message = fixture.TestHarness.Published.Select<RequestOrderAgvAssign>().First().Context.Message;
            Assert.AreEqual(_orders[0].ToGuid(), message.CorrelationId);
        }

        /// <summary>
        /// Mock order registry
        /// </summary>
        /// <param name="noCreationDates"></param>
        /// <param name="empty"></param>
        /// <returns></returns>
        private Mock<IOrderRegistry> GetOrderRegistry(bool noCreationDates = false, bool empty = false)
        {
            var registry = new Mock<IOrderRegistry>();

            var orders = new List<IOrder>();
            if (!empty)
            {
                for (int i = 0; i < _orders.Length; i++)
                {
                    var order = new OrderDetails(_orders[i].ToGuid(), _orders[i], _sources[i], _destinations[i], _leNo[i], HostOrderCreator.Simulation, noCreationDates ? DateTime.Now : _creationDates[i]);
                    orders.Add(new OrderSaga(_orders[i].ToGuid(), order, _states[i], null));
                }
            }
            registry.Setup(x => x.GetAllOrders()).Returns(orders.AsQueryable);
            registry.Setup(x => x.GetUnassignedOrders()).Returns(orders.Where(x => x.CurrentState == nameof(OrderStateMachine.OrderReceived)).OrderBy(x => x.OrderDetails.CreationDate).AsQueryable);

            return registry;
        }

        /// <summary>
        /// Mock agv registry
        /// </summary>
        /// <returns></returns>
        private Mock<IAgvRegistry> GetAgvRegistry()
        {
            var registry = new Mock<IAgvRegistry>();

            var agvs = new List<IAgv>();
            const string mapName = "Map#1";

            // Arrange
            string node01 = _sources[0];
            string node02 = _sources[1];
            string node03 = _destinations[0];
            string node04 = _destinations[1];
            string node05 = _sources[2];
            string node06 = _destinations[2];
            var agvGraph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node(node01){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 0f, Z = 0f},
                    new AgvGraph.Node(node02){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 4f, Y = 0f, Z = 0f},
                    new AgvGraph.Node(node03){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 3f, Z = 0f},
                    new AgvGraph.Node(node04){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 4f, Y = 3f, Z = 0f},
                    new AgvGraph.Node(node05){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 6f, Y = 9f, Z = 0f},
                    new AgvGraph.Node(node06){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 13f, Y = 5f, Z = 0f}
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

            for (int i = 0; i < _agvIds.Length; i++)
            {
                string agvId = _agvIds[i];
                string order = _orders[i];
                string state = _agvStates[i];
                string ip = _ips[i];
                var correlationId = agvId.ToGuid();

                var agvStatus = new AgvStatus(correlationId, order, true, true, 10, true, true);
                var agvData = new AgvData(correlationId, "Karis", "BF1", ip, 1.8, 120.0, 0.6, 0.8, 0.7, 1, 2.5, 1000, Array.Empty<AgvData.Sensor>());

                agvs.Add(new AgvSaga(correlationId, agvId, state, agvStatus, agvGraph, agvData) { Alive = true });
            }

            registry.SetupGet(x => x.Count).Returns(agvs.Count);
            registry.SetupGet(x => x.CountIdle).Returns(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle)).Count);
            registry.Setup(x => x.GetIdleAgvs()).Returns( new EnumerableQuery<IAgv>(agvs.Where(x => x.CurrentState == nameof(AgvStateMachine.Idle))));
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
    }
}
