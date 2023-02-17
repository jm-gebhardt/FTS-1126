using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Core.States;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.TransportOrder.CoreTest.Services
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
        [TestMethod]
        public async Task When_AgvRegistryIsEmpty_IAgvAssignFailed_isPublished()
        {
            var fixture = new AssignAgvConsumerMassTransitFixture()
                .AgvRegistryMock(r => r.Setup(s => s.GetReadyAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>())))
                .Build();

            string[] sources = AssignAgvConsumerMassTransitFixture.Sources;
            string[] destinations = AssignAgvConsumerMassTransitFixture.Destinations;

            string orderId = "0001";
            var correlationId = orderId.ToGuid();
            var order = new OrderDetails(correlationId, orderId, sources[0], destinations[0], "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness!.Bus.Publish(new AssignAgv(correlationId, order));

            (await fixture.ConsumerHarness!.Consumed.Any<AssignAgv>()).ShouldBeTrue();

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            (await fixture.TestHarness.Published.Any<AgvAssignmentSucceeded>()).ShouldBeFalse();
            (await fixture.TestHarness.Published.Any<AgvAssignmentFailed>()).ShouldBeTrue();
        }

        /// <summary>
        /// Receive AssignAgv message when no Ready AGV is available
        /// </summary>
        [TestMethod]
        public async Task When_NoReadyAgvIsAvailable_AgvAssignFailedIsPublished()
        {
            const string agvId = "AGV001";
            var agvCorrelationId = agvId.ToGuid();

            var fixture = new AssignAgvConsumerMassTransitFixture()
                .SetupBusRegistrationConfigurator(x =>
                {
                    x.AddRequestClient<ReserveAgv>();
                    x.AddConsumer<AgvReservationRejectedConsumer>();
                })
                .AgvRegistryMock(r => r.Setup(s => s.GetReadyAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>
                {
                    new AgvSaga(agvCorrelationId, agvId, nameof(AgvStateMachine.Ready), agvGraph: AssignAgvConsumerMassTransitFixture.Graph)
                })))
                .Build();

            string[] sources = AssignAgvConsumerMassTransitFixture.Sources;
            string[] destinations = AssignAgvConsumerMassTransitFixture.Destinations;

            const string orderId = "0001";
            var correlationId = orderId.ToGuid();
            var order = new OrderDetails(correlationId, orderId, sources[0], destinations[0], "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness!.Bus.Publish(new AssignAgv(correlationId, order));

            (await fixture.ConsumerHarness!.Consumed.Any<AssignAgv>()).ShouldBeTrue();

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            (await fixture.TestHarness.Published.Any<AgvAssignmentSucceeded>()).ShouldBeFalse();

            (await fixture.TestHarness.Published.Any<AgvAssignmentFailed>()).ShouldBeTrue();
        }

        /// <summary>
        /// Receive AssignAgv message and assign order to Ready AGV
        /// </summary>
        /// <param name="alive">Influence AGV alive state</param>
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task When_ReadyAgvIsAvailable_AgvAssignSucceededWhenAliveElseFailed(bool alive)
        {
            const string agvId = "AGV001";
            var agvCorrelationId = agvId.ToGuid();

            var fixture = new AssignAgvConsumerMassTransitFixture()
                .AgvRegistryMock(m => m.Setup(s => s.GetReadyAgvs()).Returns(new EnumerableQuery<IAgv>(new List<IAgv>
                {
                    new AgvSaga(agvCorrelationId, agvId, nameof(AgvStateMachine.Ready), agvGraph: AssignAgvConsumerMassTransitFixture.Graph) { Alive = alive }
                })))
                .SetupBusRegistrationConfigurator(x =>
                {
                    x.AddRequestClient<ReserveAgv>();
                    x.AddConsumer<AgvReservationSucceededConsumer>();
                })
                .Build();

            string[] orders = AssignAgvConsumerMassTransitFixture.Orders;
            string[] sources = AssignAgvConsumerMassTransitFixture.Sources;
            string[] destinations = AssignAgvConsumerMassTransitFixture.Destinations;
            string[] leNo = AssignAgvConsumerMassTransitFixture.LeNo;

            var correlationId = orders[0].ToGuid();
            var order = new OrderDetails(correlationId, orders[0], sources[0], destinations[0], leNo[0], HostOrderCreator.Simulation);

            await fixture.TestHarness!.Bus.Publish(new AssignAgv(correlationId, order));

            (await fixture.ConsumerHarness!.Consumed.Any<AssignAgv>()).ShouldBeTrue();

            if (alive)
            {
                // When there is an idle AGV with alive state we expect AgvAssignmentSucceeded
                var message = fixture.TestHarness.Published.Select<AgvAssignmentSucceeded>(s => s.Context.CorrelationId == correlationId).First().Context.Message;
                message.AgvId.ShouldBe(agvId);
            }
            else
            {
                // When there is just one idle AGV (but offline) we expect AgvAssignmentFailed
                var message = fixture.TestHarness.Published.Select<AgvAssignmentFailed>(s => s.Context.CorrelationId == correlationId).FirstOrDefault()?.Context.Message;
                message.ShouldNotBeNull();
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
                {
                    /*   Map#1:  (AGV_01) 1   2
                     *                       /
                     *                      / (ORD_2-3, but this edge doesn't exist)
                     *                     /
                     *                    3   4 (AGV_02) */
                    new AgvGraph.Node(node01) { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 0f, Z = 0f },
                    new AgvGraph.Node(node02) { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 0f, Z = 0f },
                    new AgvGraph.Node(node03) { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 0f, Y = 3f, Z = 0f },
                    new AgvGraph.Node(node04) { Enabled = true, Type = AgvGraph.NodeType.PickDrop, X = 4f, Y = 3f, Z = 0f }
                }, new[]
                {
                    new AgvGraph.Edge(node01, node02) { Metric = 4f }, // Used by AGV_01 to travel to source
                    new AgvGraph.Edge(node01, node03) { Metric = 3f },
                    new AgvGraph.Edge(node01, node04) { Metric = 5f },
                    new AgvGraph.Edge(node02, node01) { Metric = 4f },
                    new AgvGraph.Edge(node02, node04) { Metric = 6f },
                    new AgvGraph.Edge(node03, node01) { Metric = 3f },
                    new AgvGraph.Edge(node03, node04) { Metric = 4f },
                    new AgvGraph.Edge(node04, node01) { Metric = 5f },
                    new AgvGraph.Edge(node04, node02) { Metric = 6f }, // Used by AGV_02 to travel to source (more expensive)
                    new AgvGraph.Edge(node04, node03) { Metric = 4f }
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

            var fixture = new AssignAgvConsumerMassTransitFixture()
                .SetupBusRegistrationConfigurator(x =>
                {
                    x.AddRequestClient<ReserveAgv>();
                    x.AddConsumer<AgvReservationSucceededConsumer>();
                })
                .AgvRegistryMock(m => m.Setup(mReg => mReg.GetReadyAgvs()).Returns(
                    new EnumerableQuery<IAgv>(
                        new List<IAgv>
                        {
                            new AgvSaga(closestAgvCorrelationId, agvId, nameof(AgvStateMachine.Ready), agvGraph: agvGraph, agvStatus: agvStatus1) { Alive = aliveAgv1 },
                            new AgvSaga(agvCorrelationId2, agvId2, nameof(AgvStateMachine.Ready), agvGraph: agvGraph, agvStatus: agvStatus2) { Alive = true }
                        }
                    )))
                .Build();

            // Act
            await fixture.TestHarness!.Bus.Publish(new AssignAgv(order.CorrelationId, order));

            // Assert
            (await fixture.TestHarness.Published.Any<AssignAgv>()).ShouldBeTrue();

            (await fixture.ConsumerHarness!.Consumed.Any<AssignAgv>()).ShouldBeTrue();
            var consumedIAssignAgvMessage = fixture.TestHarness.Consumed.Select<AssignAgv>().First().Context.Message;
            consumedIAssignAgvMessage.Order.CorrelationId.ShouldBe(order.CorrelationId);

            (await fixture.TestHarness.Published.Any<AgvAssignmentSucceeded>()).ShouldBeTrue();
            var response = fixture.TestHarness.Published.Select<AgvAssignmentSucceeded>().First().Context.Message;
            if (aliveAgv1)
            {
                response.AgvCorrelationId.ShouldBe(closestAgvCorrelationId);
                response.Nodes.ShouldBe(new[] { agvStatus1.LastNodeId, node02, node01, node03 });
            }
            else
            {
                response.AgvCorrelationId.ShouldBe(agvCorrelationId2);
                response.Nodes.ShouldBe(new[] { agvStatus2.LastNodeId, node02, node01, node03 });
            }
        }
    }
}