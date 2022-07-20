using Microsoft.VisualStudio.TestTools.UnitTesting;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    /// <summary>
    /// Tests for <see cref="MqttGraph"/> and <see cref="AgvGraphReceived"/>.
    /// </summary>
    [TestClass]
    public class AgvGraphConsumerTest
    {
        private const string AgvTestIdentifier = "#0815#";

        /// <summary>
        /// Test for consuming invalid AGV graph message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveGraphMessage_WithInvalidContent_NoMessageSend()
        {
            var fixture = new MqttConsumerTestFixture<AgvGraphConsumer>();

            var message = new MqttGraph("", DateTimeOffset.UtcNow, 0, 0, Array.Empty<Node>(), Array.Empty<Edge>());

            await fixture.TestHarness.Bus.PublishMqtt(message, null);

            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvGraphReceived>());
        }

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

            var message = new MqttGraph(AgvTestIdentifier, DateTimeOffset.UtcNow, 2, 1, nodes, edges);

            await fixture.TestHarness.Bus.PublishMqtt(message, null);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<MqttGraph>());

            var consumedGraph = fixture.ConsumerHarness.Consumed.Select<MqttGraph>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier, consumedGraph.SerialNumber);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvGraphReceived>());

            var publishedGraph = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier.ToGuid(), publishedGraph.CorrelationId);
            Assert.AreEqual(AgvTestIdentifier, publishedGraph.AgvId);
            Assert.AreEqual(2, publishedGraph.Graph.Nodes.Length);
            Assert.AreEqual(1, publishedGraph.Graph.Edges.Length);
        }
    }
}
