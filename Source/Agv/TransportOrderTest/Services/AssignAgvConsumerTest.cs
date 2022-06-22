using System.Collections.Concurrent;
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

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Test for handeling Handle IAssignAgv messages
    /// </summary>
    [TestClass]
    public class AssignAgvConsumerTest
    {
        private void InitTestMembers(out InMemoryAgvRegistry agvRegistry, out ConcurrentQueue<KeyValuePair<Guid, Order>> orderQueue, out ConsumerTestFixture<AssignAgvConsumer> fixture)
        {
            var agvRegistryTmp = new InMemoryAgvRegistry();
            var orderQueueTmp = new ConcurrentQueue<KeyValuePair<Guid, Order>>();

            fixture = new ConsumerTestFixture<AssignAgvConsumer>(
               services =>
               {
                   services.AddSingleton<IAgvRegistry>((_) => agvRegistryTmp);
                   services.AddSingleton(orderQueueTmp);
               }
            );

            agvRegistry = agvRegistryTmp;
            orderQueue = orderQueueTmp;
        }

        /// <summary>
        /// Receive IAssignAgv message when no Idle AGV is available
        /// </summary>
        [TestMethod]
        public async Task ReceiveMessageWhenAgvRegistryIsEmpty()
        {
            InitTestMembers(out var _, out ConcurrentQueue<KeyValuePair<Guid, Order>> orderQueue, out ConsumerTestFixture<AssignAgvConsumer> fixture);

            string orderId = "0001";
            var correlationId = orderId.ToGuid();
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(new
            {
                CorrelationId = correlationId,
                Order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
            });

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await fixture.TestHarness.Published.Any<IAgvAssignSucceeded>());

            Assert.AreEqual(1, orderQueue.Count); // No assignment -> Order added to wait queue
        }

        /// <summary>
        /// Receive IAssignAgv message when no Idle AGV is available
        /// </summary>
        [TestMethod]
        public async Task ReceiveMessageWhenNoIdleAgvIsAvailable()
        {
            InitTestMembers(out var agvRegistry, out ConcurrentQueue<KeyValuePair<Guid, Order>> orderQueue, out ConsumerTestFixture<AssignAgvConsumer> fixture);

            string agvId = "AGV001";
            agvRegistry.AddOrUpdate(agvId.ToGuid(), new Common.Messages.AgvManagement.Agv(agvId, AgvState.Busy));

            string orderId = "0001";
            var correlationId = orderId.ToGuid();
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(new
            {
                CorrelationId = correlationId,
                Order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
            });

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await fixture.TestHarness.Published.Any<IAgvAssignSucceeded>());

            Assert.AreEqual(1, orderQueue.Count); // No assignment -> Order added to wait queue
        }

        /// <summary>
        /// Receive IAssignAgv message and assign order to Idle AGV
        /// </summary>
        [TestMethod]
        public async Task ReceiveMessageAssignOrderToIdleAgv()
        {
            InitTestMembers(out var agvRegistry, out ConcurrentQueue<KeyValuePair<Guid, Order>> orderQueue, out ConsumerTestFixture<AssignAgvConsumer> fixture);

            string agvId = "AGV001";
            agvRegistry.AddOrUpdate(agvId.ToGuid(), new Common.Messages.AgvManagement.Agv(agvId, AgvState.Idle));

            string orderId = "0001";
            var correlationId = orderId.ToGuid();
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(new
            {
                CorrelationId = correlationId,
                Order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
            });

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            var message = fixture.TestHarness.Published.Select<IAgvAssignSucceeded>(s => s.Context.CorrelationId == correlationId).First().Context.Message;
            Assert.AreEqual(agvId, message.AssignedAgv);
            Assert.AreEqual(0, orderQueue.Count); // Order not queued -> count stays the same
        }
    }
}