using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Commands;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using MassTransit;
using MassTransit.Serialization;
using MassTransit.Testing;
using MassTransit.Testing.Implementations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    /// <summary>
    /// Test class for consuming an ExecuteOrder message and publish an MqttOrder Mqtt message with proper bus configuration
    /// </summary>
    [TestClass]
    public class ExecuteTransportOrderConsumerProdTest
    {
        /// <summary>
        /// Test consuming a ExecuteOrder message and publish an MqttOrder Mqtt message with proper bus configuration
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Test_Send_TransportOrder_To_AGV_Via_MQTT()
        {
            var fixture = new MqttConsumerTestFixture<ExecuteTransportOrderConsumer>();

            // Create a publish observer
            var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
            using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
            {
                const string orderId = "1234";
                var correlationId = orderId.ToGuid();
                const string agvId = "agv0";
                var agvCorrelationId = agvId.ToGuid();
                const string fromStationId = "Pick1";
                const string toStationId = "Drop5";
                const string leNo = "na";

                var orderDetails = new OrderDetails(correlationId, orderId, fromStationId, toStationId, leNo, HostOrderCreator.Simulation);
                var message = new ExecuteTransportOrder(agvCorrelationId, orderDetails, agvId, Array.Empty<string>());

                // Publish ExecuteTransportOrder
                await fixture.TestHarness.Bus.Publish(message);

                // Check if sent
                Assert.IsTrue(await fixture.TestHarness.Published.Any<ExecuteTransportOrder>());

                // Get Consumer Harness
                IConsumerTestHarness<ExecuteTransportOrderConsumer> consumerHarness = fixture.TestHarness.GetConsumerHarness<ExecuteTransportOrderConsumer>();

                // Check if consumed
                Assert.IsTrue(await consumerHarness.Consumed.Any<ExecuteTransportOrder>());

                // Get consumed message
                IReceivedMessage<ExecuteTransportOrder> consumedMessage = consumerHarness.Consumed.Select<ExecuteTransportOrder>().First();

                // Check consumed message
                Assert.AreEqual(agvCorrelationId, consumedMessage.Context.Message.CorrelationId);
                Assert.AreEqual(agvId, consumedMessage.Context.Message.AgvId);
                Assert.AreEqual(orderId, consumedMessage.Context.Message.OrderDetails.OrderId);
                Assert.AreEqual(fromStationId, consumedMessage.Context.Message.OrderDetails.Source);

                // Get published MqttOrder Mqtt message
                IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

                // Check routing key and content type
                var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
                Assert.AreEqual(routingKeySpecified?.RoutingKeyValue, publishedMessage.Context.RoutingKey());
                Assert.AreEqual(SystemTextJsonRawMessageSerializer.JsonContentType, publishedMessage.Context.ContentType);

                // Check content
                var mqttMessage = publishedMessage.Context.Message;

                Assert.AreEqual(agvId, mqttMessage.SerialNumber);
                Assert.AreEqual(fromStationId, mqttMessage.Stations?.FromStationIdentifier);
                Assert.AreEqual(toStationId, mqttMessage.Stations?.ToStationIdentifier);
                Assert.AreEqual(OrderType.Transport, mqttMessage.OrderType);
                Assert.AreEqual(OrderRecordType.Order, mqttMessage.RecordType);
            }
        }

        /// <summary>
        /// Test consuming an SendAgvChargeOrder message and publish an MqttOrder Mqtt message with proper bus configuration
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Test_Send_ChargeOrder_To_AGV_Via_MQTT()
        {
            var fixture = new MqttConsumerTestFixture<ExecuteTransportOrderConsumer>();

            // Create a publish observer
            var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
            using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
            {
                const string agvId = "agv0";
                const string chargerId = "CHG1";

                // Publish ExecuteTransportOrder
                await fixture.TestHarness.Bus.Publish(new SendAgvChargeOrder(Guid.NewGuid(), agvId, chargerId));

                // Check if sent
                Assert.IsTrue(await fixture.TestHarness.Published.Any<SendAgvChargeOrder>());

                // Get Consumer Harness
                IConsumerTestHarness<ExecuteTransportOrderConsumer> consumerHarness = fixture.TestHarness.GetConsumerHarness<ExecuteTransportOrderConsumer>();

                // Check if consumed
                Assert.IsTrue(await consumerHarness.Consumed.Any<SendAgvChargeOrder>());

                // Get consumed message
                IReceivedMessage<SendAgvChargeOrder> consumedMessage = consumerHarness.Consumed.Select<SendAgvChargeOrder>().First();

                // Check consumed message
                Assert.AreEqual(agvId, consumedMessage.Context.Message.AgvId);
                Assert.AreEqual(chargerId, consumedMessage.Context.Message.ToCharger);
                Guid? correlationId = consumedMessage.Context.CorrelationId;

                // Get published MqttOrder Mqtt message
                IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

                // Check routing key and content type
                var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
                Assert.AreEqual(routingKeySpecified?.RoutingKeyValue, publishedMessage.Context.RoutingKey());
                Assert.AreEqual(SystemTextJsonRawMessageSerializer.JsonContentType, publishedMessage.Context.ContentType);

                // Check content
                Assert.AreEqual(agvId, publishedMessage.Context.Message.SerialNumber);
                Assert.AreEqual(correlationId.ToString(), publishedMessage.Context.Message.OrderId);
                Assert.AreEqual(chargerId, publishedMessage.Context.Message.Stations?.ToStationIdentifier);
            }
        }
    }
}
