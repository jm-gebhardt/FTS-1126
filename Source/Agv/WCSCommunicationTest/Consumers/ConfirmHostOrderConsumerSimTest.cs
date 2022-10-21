using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.HostOrder.Events;
using Gebhardt.StoreWare.Agv.WCSCommunication.Services;

namespace Gebhardt.StoreWare.Agv.WCSCommunicationTest.Consumers
{
    /// <summary>
    /// Test for finishing host orders
    /// </summary>
    [TestClass]
    public class ConfirmHostOrderConsumerSimTest
    {
        [TestMethod]
        public async Task SimulationConfirmHostOrderConsumerTest()
        {
            var fixture = new MassTransitTestFixture<SimulationConfirmHostOrderConsumer>();

            string orderId = "TestOrder123";
            var correlationId = orderId.ToGuid();

            await fixture.TestHarness!.Bus.Publish(new HostOrderHandlingSucceeded(correlationId, orderId));

            Assert.IsTrue(await fixture.TestHarness.Published.Any<HostOrderHandlingSucceeded>());
            Assert.IsTrue(await fixture.TestHarness.Consumed.Any<HostOrderHandlingSucceeded>());

            var message = fixture.ConsumerHarness!.Consumed.Select<HostOrderHandlingSucceeded>().First().Context.Message;

            Assert.AreEqual(correlationId, message.CorrelationId);
            Assert.AreEqual(orderId, message.OrderId);
        }
    }
}
