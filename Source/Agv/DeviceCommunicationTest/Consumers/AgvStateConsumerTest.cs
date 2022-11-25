using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
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

            var consumedState = fixture.ConsumerHarness.Consumed.Select<MqttState>().First().Context.Message;
            Assert.AreEqual(agvTestIdentifier, consumedState.SerialNumber);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvStatusReceived>());

            var publishedStatus = fixture.TestHarness.Published.Select<AgvStatusReceived>().First().Context.Message;
            Assert.AreEqual(agvTestIdentifier.ToGuid(), publishedStatus.CorrelationId);
            Assert.AreEqual(agvTestIdentifier, publishedStatus.AgvId);
            Assert.AreEqual(orderId, publishedStatus.AgvStatus.OrderId);
        }

        /// <summary>
        /// Test for publishing IAgvStatusReceived message with LastNodeId.
        /// </summary>
        [TestMethod]
        [DataRow("Node01", "Node01")]
        [DataRow(null, null)]
        [DataRow("", null)]
        public async Task ReceiveStateWithLastNode_ExpectLastNodeTransfered(string sentLastNodeId, string receivedLastNodeId)
        {
            var fixture = new MqttConsumerTestFixture<AgvStateConsumer>();
            const string orderId = "Order123";
            
            await fixture.TestHarness.Bus.PublishMqtt(new MqttState(AgvTestIdentifier02, orderId)
            {
                LastNodeId = sentLastNodeId,
                AgvPosition = null
            }, null);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttState>());

            var consumedState = fixture.ConsumerHarness.Consumed.Select<MqttState>().First().Context.Message;
            Assert.AreEqual(sentLastNodeId, consumedState.LastNodeId);

            var publishedStatus = fixture.TestHarness.Published.Select<AgvStatusReceived>().First().Context.Message;
            Assert.AreEqual(receivedLastNodeId, publishedStatus.AgvStatus.LastNodeId);

            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvPositionReceived>());
        }

        [TestMethod]
        [DataRow(null, false, 4.56f, 7.89f, 3.14159f)]
        [DataRow(0.0123f, true, 4.56f, 7.89f, 3.14159f)]
        public async Task ReceiveStateWithPosition_ExpectAdditionalAgvPositionReceived(float? deviationRange, bool positionInitialized, float x, float y, float theta)
        {
            var fixture = new MqttConsumerTestFixture<AgvStateConsumer>();
            const string orderId = "Order123";

            await fixture.TestHarness.Bus.PublishMqtt(new MqttState(AgvTestIdentifier02, orderId)
            {
                AgvPosition = new AgvPosition()
                {
                    DeviationRange = deviationRange,
                    PositionInitialized = positionInitialized,
                    Theta = theta,
                    X = x,
                    Y = y
                }
            }, null);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttState>());

            var consumedState = fixture.ConsumerHarness.Consumed.Select<MqttState>().First().Context.Message;
            Assert.IsNotNull(consumedState.AgvPosition);
            if (deviationRange == null)
            {
                Assert.IsTrue(consumedState.AgvPosition.DeviationRange == null);
            }
            else
            {
                Assert.AreEqual(deviationRange.Value, consumedState.AgvPosition.DeviationRange!.Value, 0.00001);
            }
            Assert.AreEqual(positionInitialized, consumedState.AgvPosition.PositionInitialized);
            Assert.AreEqual(theta, consumedState.AgvPosition.Theta, 0.00001);
            Assert.AreEqual(x, consumedState.AgvPosition.X, 0.00001);
            Assert.AreEqual(y, consumedState.AgvPosition.Y, 0.00001);

            var publishedPos = fixture.TestHarness.Published.Select<AgvPositionReceived>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier02.ToGuid(), publishedPos.CorrelationId);
            Assert.AreEqual(AgvTestIdentifier02, publishedPos.AgvId);
            Assert.AreEqual(positionInitialized, publishedPos.AgvPosition.PositionInitialized);
            Assert.AreEqual(Mathematics.ConvertToRadians(90) - theta, publishedPos.AgvPosition.Theta);
            Assert.AreEqual(x, publishedPos.AgvPosition.X);
            Assert.AreEqual(y, publishedPos.AgvPosition.Y * -1);
        }
    }
}
