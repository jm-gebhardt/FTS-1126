using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.States;
using JetBrains.Annotations;
using MassTransit;
using Moq;

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
        private const string Src01 = "SRC1";
        private const string Dst01 = "DST1";
        private static AgvGraph? _graph;


        /// <summary>
        /// Test initialization.
        /// </summary>
        /// <param name="context"></param>
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _graph = new AgvGraph(Guid.Empty,
                new[]
                {
                    new AgvGraph.Node(Src01) { Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 0f, Z = 0f },
                    new AgvGraph.Node(Dst01) { Enabled = true, Type = AgvGraph.NodeType.Station, X = 1f, Y = 1f, Z = 0f }
                },
                new[]
                {
                   new AgvGraph.Edge(Src01, Dst01) { Metric = 1f }
                });
        }

		/// <summary>
		/// Test cleanup.
		/// </summary>
		[ClassCleanup]
		public static void ClassCleanup()
		{
		}

		/// <summary>
		/// Receive AssignAgv message when no Idle AGV is available
		/// </summary>
		[TestMethod]
        public async Task When_AgvRegistryIsEmpty_IAgvAssignFailed_isPublished()
        {
            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetIdleAgvs()).Returns(new List<IAgv>());
            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(
                services: s => { s.AddTransient(_ => mockRegistry.Object); }
                );

            string orderId = "0001";
            var correlationId = orderId.ToGuid();
            var order = new Order(correlationId, orderId, Src01, Dst01, "LeNo123", HostOrderCreator.Simulation);

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
            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetIdleAgvs()).Returns(new List<IAgv>
            {
                new AgvSaga(agvCorrelationId, agvId, nameof(AgvStateMachine.Idle), agvGraph: _graph)
            });

            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(
                services: s => { s.AddSingleton((_) => mockRegistry.Object); },
                busRegistrationConfigurator: x =>
                {
                    x.AddRequestClient<ReserveAgv>();
                    x.AddConsumer<AgvReservationRejectedConsumer>();
                }
            );

            const string orderId = "0001";
            var correlationId = orderId.ToGuid();
            var order = new Order(correlationId, orderId, Src01, Dst01, "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new AssignAgv(correlationId, order));

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvAssignmentSucceeded>());

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvAssignmentFailed>());
        }

        /// <summary>
        /// Receive AssignAgv message and assign order to Idle AGV
        /// </summary>
        [TestMethod]
        public async Task When_IdleAgvIsAvailable_AgvAssignSucceededIsPublished()
        {
            const string agvId = "AGV001";
            var agvCorrelationId = agvId.ToGuid();
            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(s => s.GetIdleAgvs()).Returns(new List<IAgv>
            {
                new AgvSaga(agvCorrelationId, agvId, nameof(AgvStateMachine.Idle), agvGraph: _graph)
            });

            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(
                services: s => { s.AddSingleton((_) => mockRegistry.Object); },
                busRegistrationConfigurator: x =>
                {
                    x.AddRequestClient<ReserveAgv>();
                    x.AddConsumer<AgvReservationSucceededConsumer>();
                }
            );

            const string orderId = "0001";
            var correlationId = orderId.ToGuid();
            var order = new Order(correlationId, orderId, Src01, Dst01, "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new AssignAgv(correlationId, order));

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());

            var message = fixture.TestHarness.Published.Select<AgvAssignmentSucceeded>(s => s.Context.CorrelationId == correlationId).First().Context.Message;
            Assert.AreEqual(agvId, message.AgvId);
        }
        
        /// <summary>
        /// Checks IAssignAgv consumption and calculation with LastNodeId
        /// </summary>
        [TestMethod]
        public async Task ConsumeIAssignAgv_and_determine_Agv_with_LastNodeId()
        {
            // Arrange
            const string node01 = "node01";
            const string node02 = "node02";
            const string node03 = "node03";
            const string node04 = "node04";
            var agvGraph = new AgvGraph(Guid.Empty,
                new[]
            {
                new AgvGraph.Node(node01){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 0f, Z = 0f},
                new AgvGraph.Node(node02){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 4f, Y = 0f, Z = 0f},
                new AgvGraph.Node(node03){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 0f, Y = 3f, Z = 0f},
                new AgvGraph.Node(node04){ Enabled = true, Type = AgvGraph.NodeType.Station, X = 4f, Y = 3f, Z = 0f}
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
            });

            const string agvId = "AGV_01";
            var closestAgvCorrelationId = agvId.ToGuid();
            var agvStatus1 = new AgvStatus(closestAgvCorrelationId, null, false, true, 50, false, false, node01);
            const string agvId2 = "AGV_02";
            var agvCorrelationId2 = agvId2.ToGuid();
            var agvStatus2 = new AgvStatus(closestAgvCorrelationId, null, false, true, 50, false, false, node04);

            var mockRegistry = new Mock<IAgvRegistry>();
            mockRegistry.Setup(mReg => mReg.GetIdleAgvs()).Returns(
                new List<IAgv>
                {
                    new AgvSaga(closestAgvCorrelationId, agvId, nameof(AgvStateMachine.Idle), agvGraph: agvGraph, agvStatus: agvStatus1),
                    new AgvSaga(agvCorrelationId2, agvId2, nameof(AgvStateMachine.Idle), agvGraph: agvGraph, agvStatus: agvStatus2)
                }
            );

            const string orderId = "order123";
            const string source = node02;
            const string destination = node03;
            const string leNo = "LeNo123";
            var order = new Order(
                orderId.ToGuid(),
                orderId,
                source,
                destination,
                leNo,
                HostOrderCreator.Simulation
            );

            var fixture = new MassTransitTestFixture<AssignAgvConsumer>(
	            s => { s.AddTransient(_ => mockRegistry.Object); }, 
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
            Assert.AreEqual(closestAgvCorrelationId, response.AgvCorrelationId);
        }
    }
}
