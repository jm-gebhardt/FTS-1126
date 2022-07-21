using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Round Robin AgvId Dispatcher Tests
    /// </summary>
    [TestClass]
    public class RoundRobinAgvDispatcherTest
    {
        private const string AgvNamePrefix = "AGV";

        /// <summary/>
        [TestMethod]
        public async Task Respond_To_AgvAssigned_Using_SingleAGV()
        {
            var fixture = new MassTransitTestFixture<RoundRobinAssignAgvConsumer>(
                services =>
                {
                    services.AddSingleton(typeof(IAgvRegistry), new RoundRobinAgvRegistry(new SimulateOptions() { Agvs = 1 }));
                }
            );

            const string agvId = $"{AgvNamePrefix}_000";
            var agvCorrelationId = agvId.ToGuid();

            // ----------- First Assignment ---------------------------------
            const string orderId1 = "Order#1";
            var correlationId1 = orderId1.ToGuid();
            var order1 = new Order(correlationId1, orderId1, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new AssignAgv(correlationId1, order1));

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());

            var responseMessage = fixture.TestHarness.Published.Select<AgvAssignmentSucceeded>().First().Context.Message;
            Assert.IsNotNull(responseMessage);
            Assert.AreEqual(correlationId1, responseMessage.CorrelationId);
            Assert.AreEqual(agvId, responseMessage.AgvId);
            Assert.AreEqual(agvCorrelationId, responseMessage.AgvCorrelationId);

            // ----------- Second Assignment ---------------------------------
            const string orderId2 = "Order#2";
            var correlationId2 = orderId2.ToGuid();
            var order2 = new Order(correlationId2, orderId2, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new AssignAgv(correlationId2, order2));

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());

            responseMessage = fixture.TestHarness.Published.Select<AgvAssignmentSucceeded>(
                s => s.Context.CorrelationId == correlationId2).First().Context.Message;
            Assert.IsNotNull(responseMessage);
            Assert.AreEqual(correlationId2, responseMessage.CorrelationId);
            Assert.AreEqual(agvId, responseMessage.AgvId);
            Assert.AreEqual(agvCorrelationId, responseMessage.AgvCorrelationId);
        }

        /// <summary>
        /// Respond_To_AgvAssigned_MultipleAgvCheckRoundRobinOverFlow
        /// </summary>
        [TestMethod]
        public async Task Respond_To_AgvAssigned_MultipleAgvCheckRoundRobinOverFlow()
        {
            var fixture = new MassTransitTestFixture<RoundRobinAssignAgvConsumer>(
                services =>
                {
                    services.AddSingleton(typeof(IAgvRegistry), new RoundRobinAgvRegistry(new SimulateOptions() { Agvs = 2 }));
                }
            );

            const string agvId0 = $"{AgvNamePrefix}_000";
            const string agvId1 = $"{AgvNamePrefix}_001";

            // ----------- First Assignment ---------------------------------
            const string orderId1 = "Order#1";
            var correlationId1 = orderId1.ToGuid();
            var order1 = new Order(correlationId1, orderId1, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new AssignAgv(correlationId1, order1));

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());

            var responseMessage = fixture.TestHarness.Published.Select<AgvAssignmentSucceeded>().First().Context.Message;
            Assert.AreEqual(agvId0, responseMessage.AgvId);

            // ----------- Second Assignment ---------------------------------
            const string orderId2 = "Order#2";
            var correlationId2 = orderId2.ToGuid();
            var order2 = new Order(correlationId2, orderId2, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new AssignAgv(correlationId2, order2));

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());

            responseMessage = fixture.TestHarness.Published.Select<AgvAssignmentSucceeded>(
                s => s.Context.CorrelationId == correlationId2).First().Context.Message;
            Assert.AreEqual(agvId1, responseMessage.AgvId);

            // ----------- Third Assignment ---------------------------------
            const string orderId3 = "Order#3";
            var correlationId3 = orderId3.ToGuid();
            var order3 = new Order(correlationId3, orderId3, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation);

            await fixture.TestHarness.Bus.Publish(new AssignAgv(correlationId3, order3));

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<AssignAgv>());

            responseMessage = fixture.TestHarness.Published.Select<AgvAssignmentSucceeded>(
                s => s.Context.CorrelationId == correlationId3).First().Context.Message;
            Assert.AreEqual(agvId0, responseMessage.AgvId);
        }

        /// <summary>
        /// Verfy_IAgvRegistry_Interface_Methods
        /// </summary>
        [TestMethod]
        public void Verfy_IAgvRegistry_Interface_Methods()
        {
            var fixture = new MassTransitTestFixture<RoundRobinAssignAgvConsumer>(
                services =>
                {
                    services.AddSingleton(typeof(IAgvRegistry), new RoundRobinAgvRegistry(new SimulateOptions() { Agvs = 2 }));
                }
            );

            var agvRegistry = fixture.Provider.GetRequiredService<IAgvRegistry>();

            Assert.AreEqual(2, agvRegistry.Count);
            Assert.AreEqual(2, agvRegistry.CountIdle);

            IEnumerable<IAgv> allAgvs = agvRegistry.GetAllAgvs();
            IEnumerable<IAgv> idleAgvs = agvRegistry.GetIdleAgvs();

            // ReSharper disable PossibleMultipleEnumeration
            Assert.AreEqual(2, allAgvs.Count());
            Assert.AreEqual(2, idleAgvs.Count());

            foreach (var actualAgv in allAgvs)
            {
                var foundAgv = agvRegistry.GetAgv(actualAgv.CorrelationId);
                Assert.IsNotNull(foundAgv);
                Assert.AreEqual(actualAgv.AgvId, foundAgv.AgvId);
            }

            Assert.IsNull(agvRegistry.GetAgv(Guid.Empty));
        }
    }
}
