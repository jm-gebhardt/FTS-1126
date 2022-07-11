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
    /// Tests for <see cref="IGraph"/> and <see cref="AgvGraphReceived"/>.
    /// </summary>
    [TestClass]
    public class AgvGraphConsumerTest
    {
        private const string AgvTestIdentifier02 = "#0815#";


        /// <summary>
        /// Test for consuming invalid AGV graph message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveGraphMessage_WithInvalidContent_NoMessageSend()
        {
            var fixture = new MqttConsumerTestFixture<AgvGraphConsumer>();

            await fixture.TestHarness.Bus.PublishMqtt<IGraph>(
                new
                {
                    SerialNumber = "",
                    ModificationDate = DateTimeOffset.UtcNow
                }, null);

            Assert.IsFalse(await fixture.TestHarness.Published.Any<AgvGraphReceived>());
        }

        /// <summary>
        /// Test for consuming AGV graph message.
        /// </summary>
        [TestMethod]
        public async Task ReceiveGraphMessage_WithValidInput_ExpectMessage()
        {
            var fixture = new MqttConsumerTestFixture<AgvGraphConsumer>();

            await fixture.TestHarness.Bus.PublishMqtt<IGraph>(
                new
                {
                    SerialNumber = AgvTestIdentifier02,
                    ModificationDate = DateTimeOffset.UtcNow,

                    NumberOfNodes = 2,
                    Nodes = new[]
                    {
                        new Node() { NodeId = 1, X = 0f, Y = 0f, Z = 0f },
                        new Node() { NodeId = 2, X = 100f, Y = 100f, Z = 0f }
                    },

                    NumberofEdges = 1,
                    Edges = new[]
                    {
                        new Edge() { EdgeId = 1, StartId = 1, EndId = 2, Bidirectional = false, MaxVelocity = 1 }
                    }
                }, null);
            Assert.IsTrue(await fixture.TestHarness.Published.Any<IGraph>());

            var consumedGraph = fixture.ConsumerHarness.Consumed.Select<IGraph>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier02, consumedGraph.SerialNumber);

            Assert.IsTrue(await fixture.TestHarness.Published.Any<AgvGraphReceived>());

            var publishedGraph = fixture.TestHarness.Published.Select<AgvGraphReceived>().First().Context.Message;
            Assert.AreEqual(AgvTestIdentifier02.ToGuid(), publishedGraph.CorrelationId);
            Assert.AreEqual(AgvTestIdentifier02, publishedGraph.AgvId);
            Assert.AreEqual(2, publishedGraph.Graph.Nodes.Length);
            Assert.AreEqual(1, publishedGraph.Graph.Edges.Length);
        }
    }
}
