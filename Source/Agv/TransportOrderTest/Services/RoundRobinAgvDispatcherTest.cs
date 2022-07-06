using Gebhardt.StoreWare.Agv.Common.Definitions;
using Gebhardt.StoreWare.Agv.Common.Messages;
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
        
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task Respond_To_AgvAssigned_Using_SingleAGV()
        {
            var fixture = new ConsumerTestFixture<RoundRobinAssignAgvConsumer>(
                services =>
                {
                    services.AddSingleton(new RoundRobinAgvRegistry(new SimulateOptions() { Agvs = 1 }));
                }
            );
            // ----------- First Assignment ---------------------------------
            string orderId = "OrderId123";
            var correlationId = orderId.ToGuid();
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(
                new
                {
                    CorrelationId = correlationId,
                    Order = new Order(correlationId, orderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
                }
            );

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            var responseMessage = fixture.TestHarness.Published.Select<IAgvAssignmentSucceeded>().First().Context.Message;
            Assert.IsNotNull(responseMessage);
            Assert.AreEqual(correlationId, responseMessage.CorrelationId);
            Assert.AreEqual($"{AgvNamePrefix}_0", responseMessage.AgvId);
            const string secondOrderId = "Order#2";
            var secondCorrelationId = secondOrderId.ToGuid();  
            
            // ----------- Second Assignment ---------------------------------
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(
                new
                {
                    correlationId2 = secondCorrelationId,
                    Order = new Order(secondCorrelationId,secondOrderId,  "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
                }
            );

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            responseMessage = fixture.TestHarness.Published.Select<IAgvAssignmentSucceeded>().First().Context.Message;
            Assert.AreEqual($"{AgvNamePrefix}_0", responseMessage.AgvId);
        }
        
        /// <summary>
        /// Respond_To_AgvAssigned_MultipleAgvCheckRoundRobinOverFlow
        /// </summary>
        [TestMethod]
        public async Task Respond_To_AgvAssigned_MultipleAgvCheckRoundRobinOverFlow()
        {
            var fixture = new ConsumerTestFixture<RoundRobinAssignAgvConsumer>(
                services =>
                {
                    services.AddSingleton(new RoundRobinAgvRegistry(new SimulateOptions() { Agvs = 2 }));
                }
            );
            // ----------- First Assignment ---------------------------------
            const string orderId = "Order#1";
            var correlationId = orderId.ToGuid();
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(
                new
                {
                    CorrelationId = correlationId,
                    Order = new Order(correlationId, orderId,  "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
                }
            );

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            var responseMessage = fixture.TestHarness.Published.Select<IAgvAssignmentSucceeded>().First().Context.Message;
            Assert.AreEqual($"{AgvNamePrefix}_0", responseMessage.AgvId);
            // ----------- Second Assignment ---------------------------------
            const string secondOrderId = "Order#2";
            var secondCorrelationId = secondOrderId.ToGuid();
            
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(
                new
                {
                    CorrelationId = secondCorrelationId,
                    Order = new Order(secondCorrelationId,  secondOrderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
                }
            );

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            responseMessage = fixture.TestHarness.Published.Select<IAgvAssignmentSucceeded>(
                s=> s.Context.CorrelationId == secondCorrelationId).First().Context.Message;
            Assert.AreEqual($"{AgvNamePrefix}_1", responseMessage.AgvId);
            // ----------- Third Assignment ---------------------------------
            const string thirdOrderId = "Order#3";
            var thirdCorrelationId = thirdOrderId.ToGuid();
            
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(
                new
                {
                    CorrelationId = thirdCorrelationId,
                    Order = new Order(thirdCorrelationId, thirdOrderId, "SRC1", "DST1", "LeNo123", HostOrderCreator.Simulation)
                }
            );

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            responseMessage = fixture.TestHarness.Published.Select<IAgvAssignmentSucceeded>(
                s=> s.Context.CorrelationId == thirdCorrelationId).First().Context.Message;
            Assert.AreEqual($"{AgvNamePrefix}_0", responseMessage.AgvId);
        }
    }
}