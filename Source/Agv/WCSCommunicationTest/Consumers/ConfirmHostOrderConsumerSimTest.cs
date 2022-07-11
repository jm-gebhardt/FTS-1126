using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Consumers
{
	/// <summary>
	/// Test for finishing host orders
	/// </summary>
	[TestClass]
    public class ConfirmHostOrderConsumerSimTest
    {
        /// <summary>
        /// Test for processing IExecuteTransportOrder message
        /// </summary>
        [TestMethod]
        public async Task SimulationConfirmHostOrderConsumerTest()
        {
            var fixture = new MassTransitTestFixture<SimulationConfirmHostOrderConsumer>();
           
            string orderId = "TestOrder123";
            await fixture.TestHarness.Bus.Publish<IHostOrderHandlingSucceeded>(new { OrderId = orderId });

            Assert.IsTrue(await fixture.TestHarness.Published.Any<IHostOrderHandlingSucceeded>());
            Assert.AreEqual(orderId, fixture.ConsumerHarness.Consumed.Select<IHostOrderHandlingSucceeded>().First().Context.Message.OrderId);
        }
    }
}
