using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Test for handeling Handle IAssignAgv messages
    /// </summary>
    [TestClass]
    public class AgvStateChangedConsumerTest
    {
        private void InitTestMembers(out InMemoryAgvRegistry agvRegistry, out ConcurrentQueue<KeyValuePair<Guid, Order>> orderQueue, out ConsumerTestFixture<AgvStateChangedConsumer> fixture)
        {
            var agvRegistryTmp = new InMemoryAgvRegistry();
            var orderQueueTmp = new ConcurrentQueue<KeyValuePair<Guid, Order>>();

            fixture = new ConsumerTestFixture<AgvStateChangedConsumer>(
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
        /// Test consuming IAgvStateChanged and adding AGV to registry
        /// </summary>
        [TestMethod]
        public async Task MessageConsumedAgvAdded()
        {
            InitTestMembers(out var agvRegistry, out ConcurrentQueue<KeyValuePair<Guid, Order>> _, out ConsumerTestFixture<AgvStateChangedConsumer>  fixture);

            string agvId = "AGV001";
            await fixture.TestHarness.Bus.Publish<IAgvStateChanged>(new
            {
                CorrelationId = agvId.ToGuid(),
                AgvId = agvId,
                PreviousState = AgvState.Discovered,
                CurrentState = AgvState.Idle
            });

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await fixture.TestHarness.Published.Any<IAgvAssignSucceeded>()); // No waiting Order -> no assignment

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAgvStateChanged>());
            Assert.AreEqual(1, agvRegistry.Count); // AGV added
        }

        /// <summary>
        /// Test consuming IAgvStateChanged and updating existing AGV
        /// </summary>
        [TestMethod]
        public async Task MessageConsumedAgvUpdated()
        {
            InitTestMembers(out var agvRegistry, out ConcurrentQueue<KeyValuePair<Guid, Order>> _, out ConsumerTestFixture<AgvStateChangedConsumer> fixture);

            string agvId = "AGV001"; // Add AGV with state Discovered
            agvRegistry.AddOrUpdate(agvId.ToGuid(), new Common.Messages.AgvManagement.Agv(agvId, AgvState.Discovered));

            await fixture.TestHarness.Bus.Publish<IAgvStateChanged>(new
            {
                CorrelationId = agvId.ToGuid(),
                AgvId = agvId,
                PreviousState = AgvState.Discovered,
                CurrentState = AgvState.Busy // Change state to Idle
            });

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await fixture.TestHarness.Published.Any<IAgvAssignSucceeded>()); // No waiting Order -> no assignment

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAgvStateChanged>());
            Assert.AreEqual(1, agvRegistry.Count); // AGV updated -> count stays the same
        }

        /// <summary>
        /// Receive AGV with Busy state -> don't assign it to a waiting order
        /// </summary>
        [TestMethod]
        public async Task ConsumeStateForBusyAgv()
        {
            InitTestMembers(out var agvRegistry, out ConcurrentQueue<KeyValuePair<Guid, Order>> orderQueue, out ConsumerTestFixture<AgvStateChangedConsumer> fixture);

            var order1 = new Order("0001", "", "", "", HostOrderCreator.Simulation);
            var order2 = new Order("0002", "", "", "", HostOrderCreator.Simulation);

            var correlationId1 = order1.OrderId.ToGuid();
            var correlationId2 = order2.OrderId.ToGuid();

            orderQueue.Enqueue(KeyValuePair.Create(correlationId1, order1));
            orderQueue.Enqueue(KeyValuePair.Create(correlationId2, order2));
            Assert.AreEqual(2, orderQueue.Count);

            string agvId = "AGV001";
            await fixture.TestHarness.Bus.Publish<IAgvStateChanged>(new
            {
                CorrelationId = agvId.ToGuid(),
                AgvId = agvId,
                PreviousState = AgvState.Discovered,
                CurrentState = AgvState.Busy
            });

            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await fixture.TestHarness.Published.Any<IAgvAssignSucceeded>()); // AGV not Idle -> no assignment

            Assert.AreEqual(1, agvRegistry.Count); // AGV added
            Assert.AreEqual(2, orderQueue.Count); // Order count stays the same
        }

        /// <summary>
        /// Receive AGV with Idle state -> assign it to a waiting order
        /// </summary>
        [TestMethod]
        public async Task ConsumeStateForIdleAgv()
        {
            InitTestMembers(out var agvRegistry, out ConcurrentQueue<KeyValuePair<Guid, Order>> orderQueue, out ConsumerTestFixture<AgvStateChangedConsumer> fixture);

            var order1 = new Order("0001", "", "", "", HostOrderCreator.Simulation);
            var order2 = new Order("0002", "", "", "", HostOrderCreator.Simulation);

            var correlationId1 = order1.OrderId.ToGuid();
            var correlationId2 = order2.OrderId.ToGuid();

            orderQueue.Enqueue(KeyValuePair.Create(correlationId1, order1));
            orderQueue.Enqueue(KeyValuePair.Create(correlationId2, order2));
            Assert.AreEqual(2, orderQueue.Count);

            string agvId = "AGV001";
            await fixture.TestHarness.Bus.Publish<IAgvStateChanged>(new
            {
                CorrelationId = agvId.ToGuid(),
                AgvId = agvId,
                PreviousState = AgvState.Discovered,
                CurrentState = AgvState.Idle
            });

            var message = fixture.TestHarness.Published.Select<IAgvAssignSucceeded>().First().Context.Message;
            Assert.AreEqual(order1.OrderId, message.OrderId);

            Assert.AreEqual(1, agvRegistry.Count); // AGV added
            Assert.AreEqual(1, orderQueue.Count); // Order count reduced by 1
        }
    }
}