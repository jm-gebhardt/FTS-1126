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
        
        private const string AgvTestIdentifier02 = "#0815#";
        
        /// <summary>
        ///  Test for consuming invalid AGV status message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveStatusMessage_WithInvalidContent_NoMessageSend()
        {
            var fixture = new MqttConsumerTestFixture<AgvStateConsumer>();

            await fixture.TestHarness.Bus.PublishMqtt<IState>(
                new
                {
                    SerialNumber = ""
                }, null);

            Assert.IsFalse(await fixture.TestHarness.Published.Any<IAgvStatusReceived>());
            
        }
        
        /// <summary>
        /// Test for consuming AGV graph message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveGraphMessage_WithValidInput_ExpectMessage()
        {
            var fixture = new MqttConsumerTestFixture<AgvStateConsumer>();
            const string orderId = "Order123";
          
            await fixture.TestHarness.Bus.PublishMqtt<IState>(
                new
                {
                    SerialNumber = AgvTestIdentifier02,
                    OrderId = orderId
                }, null);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IState>());

            var consumedGraph = fixture.ConsumerHarness.Consumed.Select<IState>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier02, consumedGraph.SerialNumber);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<IAgvStatusReceived>());

            var publishedStatus = fixture.TestHarness.Published.Select<IAgvStatusReceived>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier02.ToGuid(), publishedStatus.CorrelationId);
            Assert.AreEqual(AgvTestIdentifier02, publishedStatus.AgvId);
            Assert.AreEqual(orderId, publishedStatus.AgvStatus.OrderId);
        }
    }

}