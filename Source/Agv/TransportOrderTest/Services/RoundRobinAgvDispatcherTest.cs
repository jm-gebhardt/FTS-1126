using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Commands;
using Gebhardt.StoreWare.Agv.Common.Messages.Agv.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.TransportOrder.CmdOptions;
using Gebhardt.StoreWare.Agv.TransportOrder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.TransportOrderTest.Services
{
    /// <summary>
    /// Round Robin Agv Dispatcher Tests
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
                    services.AddScoped(_ => new SimulateOptions() {Agvs = 2});
                } 
            );
            
            var correlationId = Guid.NewGuid();
            string orderId = "OrderId123";
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(
                new
                {
                    CorrelationId = correlationId,
                    OrderId = orderId
                }
            );

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            var responseMessage = fixture.TestHarness.Published.Select<IAgvAssignSucceeded>().First().Context.Message;
            Assert.IsNotNull(responseMessage);
            Assert.AreEqual(correlationId, responseMessage.CorrelationId);
            Assert.AreEqual(orderId, responseMessage.OrderId);
            Assert.AreEqual($"{AgvNamePrefix}_0", responseMessage.AssignedAgv);
            

            await fixture.TestHarness.Bus.Publish<IAssignAgv>(
                new
                {
                    CorrelationId = Guid.NewGuid(),
                    OrderId = Guid.NewGuid()
                }
            );

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            responseMessage = fixture.TestHarness.Published.Select<IAgvAssignSucceeded>().First().Context.Message;
            Assert.AreEqual($"{AgvNamePrefix}_0", responseMessage.AssignedAgv);
        }
        
        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public async Task Respond_To_AgvAssigned_MultipleAgvCheckRoundRobinOverFlow()
        {
            var fixture = new ConsumerTestFixture<RoundRobinAssignAgvConsumer>(
                services =>
                {
                    services.AddScoped(_ => new SimulateOptions() {Agvs = 2});
                } 
            );
            
            var correlationId = Guid.NewGuid();
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(
                new
                {
                    CorrelationId = correlationId,
                    OrderId = Guid.NewGuid()
                }
            );

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            var responseMessage = fixture.TestHarness.Published.Select<IAgvAssignSucceeded>().First().Context.Message;
            Assert.AreEqual($"{AgvNamePrefix}_0", responseMessage.AssignedAgv);

            var secondCorrelationId = Guid.NewGuid();
            await fixture.TestHarness.Bus.Publish<IAssignAgv>(
                new
                {
                    CorrelationId = secondCorrelationId,
                    OrderId = Guid.NewGuid()
                }
            );

            Assert.IsTrue(await fixture.ConsumerHarness.Consumed.Any<IAssignAgv>());

            responseMessage = fixture.TestHarness.Published.Select<IAgvAssignSucceeded>(
                s=> s.Context.CorrelationId == secondCorrelationId).First().Context.Message;
            Assert.AreEqual($"{AgvNamePrefix}_1", responseMessage.AssignedAgv);
        }
    }
}