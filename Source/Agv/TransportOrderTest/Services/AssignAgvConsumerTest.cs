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
        public async Task When_NoIdleAgvIsIdle_AgvAssignFailedisPublished()
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
    }
}
