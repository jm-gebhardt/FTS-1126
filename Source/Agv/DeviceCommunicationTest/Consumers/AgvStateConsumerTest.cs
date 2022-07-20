using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    /// <summary>
    /// AgvStateConsumerTest
    /// </summary>
    [TestClass]
    public class AgvStateConsumerTest
    {
        /// <summary>
        ///  Test for consuming invalid AGV status message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveStatusMessage_WithInvalidContent_NoMessageSend()
        {
            var fixture = new MqttConsumerTestFixture<AgvStateConsumer>();

            await fixture.TestHarness.Bus.PublishMqtt(new MqttState("", ""), null);

            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvStatusReceived>());
        }

        /// <summary>
        /// Test for consuming AGV graph message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveGraphMessage_WithValidInput_ExpectMessage()
        {
            var fixture = new MqttConsumerTestFixture<AgvStateConsumer>();
            const string agvTestIdentifier = "#0815#";
            const string orderId = "Order123";

            await fixture.TestHarness.Bus.PublishMqtt(new MqttState(agvTestIdentifier, orderId), null);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttState>());

            var consumedGraph = fixture.ConsumerHarness.Consumed.Select<MqttState>().First().Context.Message;
            Assert.AreEqual(agvTestIdentifier, consumedGraph.SerialNumber);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());

            var publishedStatus = fixture.TestHarness.Published.Select<AgvStatusReceived>().First().Context.Message;
            Assert.AreEqual(agvTestIdentifier.ToGuid(), publishedStatus.CorrelationId);
            Assert.AreEqual(agvTestIdentifier, publishedStatus.AgvId);
            Assert.AreEqual(orderId, publishedStatus.AgvStatus.OrderId);
        }
    }
}
