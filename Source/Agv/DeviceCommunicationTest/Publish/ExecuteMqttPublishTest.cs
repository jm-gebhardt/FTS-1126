using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using MassTransit;
using MassTransit.Serialization;
using MassTransit.Testing;
using MassTransit.Testing.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Publish
{
    /// <summary>
    /// Test class for publishing a MQTT message with proper bus configuration
    /// </summary>
    [TestClass]
    public class ExecuteMqttPublishTest
    {
        /// <summary>
        /// Test publishing a MQTT message with proper bus configuration
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestPublishMqttMessageAndBusConfiguration()
        {
            var fixture = new MqttTestFixture();
            var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));

            var routingKeySpecified = typeof(MqttState).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
            string serialNumber = "AGV01";
            string orderId = "1234";

            using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
            {
                await fixture.TestHarness.Bus.PublishMqtt(new MqttState(serialNumber, orderId) { OperatingMode = OperatingMode.Automatic }, null);

                IPublishedMessage<MqttState> publishedMessage = publishObserver.Messages.Select<MqttState>().First();

                Assert.AreEqual(routingKeySpecified?.RoutingKeyValue, publishedMessage.Context.RoutingKey());
                Assert.AreEqual(SystemTextJsonRawMessageSerializer.JsonContentType, publishedMessage.Context.ContentType);
                Assert.AreEqual(serialNumber, publishedMessage.Context.Message.SerialNumber);
                Assert.AreEqual(orderId, publishedMessage.Context.Message.OrderId);
                Assert.AreEqual(OperatingMode.Automatic, publishedMessage.Context.Message.OperatingMode);
            }
        }
    }
}
