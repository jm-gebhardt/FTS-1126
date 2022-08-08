using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    /// <summary>
    /// Tests for <see cref="MqttGraph"/> and <see cref="MqttGraphReceived"/>.
    /// </summary>
    [TestClass]
    public class AgvGraphConsumerTest
    {
        private const string AgvTestIdentifier = "#0815#";

        /// <summary>
        /// Test for consuming AGV graph message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveGraphMessage_WithValidInput_ExpectMessage()
        {
            var fixture = new MqttConsumerTestFixture<AgvGraphConsumer>();

            var nodes = new Node[] {
                new() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                new() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
            };
            var edges = new Edge[]
            {
                new() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
            };
            var message = new MqttGraph(AgvTestIdentifier, DateTimeOffset.UtcNow, nodes, edges, string.Empty);

            await fixture.TestHarness.Bus.PublishMqtt(message, null);

            // Did the test publish MqttGraph ...
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttGraph>());
            // ... and has the message been consumed?
            var consumed = fixture.ConsumerHarness.Consumed.Select<MqttGraph>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier, consumed.SerialNumber);

            // Did the consumer forward a Graph message ...
            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttGraphReceived>());
            // ... with expected content?
            var published = fixture.TestHarness.Published.Select<MqttGraphReceived>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier.ToGuid(), published.CorrelationId);
            Assert.AreEqual(AgvTestIdentifier, published.Graph.SerialNumber);
            Assert.AreEqual(2, published.Graph.Nodes?.Length);
            Assert.AreEqual(1, published.Graph.Edges?.Length);
        }
    }
}
