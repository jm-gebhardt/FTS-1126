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
using Gebhardt.StoreWare.Agv.Common.Messages.Actions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Commands;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    /// <summary>
    /// Test class for consuming an ExecuteOrder message and publish an MqttOrder Mqtt message with proper bus configuration
    /// </summary>
    [TestClass]
    public class StartActionCommandConsumerTest
    {
        /// <summary>
        /// Test consuming a ExecuteOrder message and publish an MqttOrder Mqtt message with proper bus configuration
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Test_Send_TransportOrder_To_AGV_Via_MQTT()
        {
            var fixture = new MqttConsumerTestFixture<StartActionCommandConsumer>();

            // Create a publish observer
            var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
            using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
            {
                const string orderId = "1234";
                var correlationId = orderId.ToGuid();
                const string agvId = "agv0";
                var agv = agvId.ToGuid();
                const string fromStationId = "Pick1";
                const string toStationId = "Drop5";
                const string leNo = "na";

                var orderDetails = new OrderDetails(correlationId, orderId, fromStationId, toStationId, leNo, HostOrderCreator.Simulation);
                var message = new StartActionCommand(agv,
                    agvId,
                    new AgvAction(orderDetails.CorrelationId,
                        ActionType.Transport,
                        orderDetails)
                );

                // Publish ExecuteTransportOrder
                await fixture.TestHarness.Bus.Publish(message);

                // Get Consumer Harness
                IConsumerTestHarness<StartActionCommandConsumer> consumerHarness = fixture.TestHarness.GetConsumerHarness<StartActionCommandConsumer>();

                // Check if consumed
                (await consumerHarness.Consumed.Any<StartActionCommand>()).ShouldBeTrue();

                // Get consumed message
                IReceivedMessage<StartActionCommand> consumedMessage = consumerHarness.Consumed.Select<StartActionCommand>().First();
                orderDetails = consumedMessage.Context.Message.Action.OrderDetails;

                // Check consumed message
                consumedMessage.Context.Message.CorrelationId.ShouldBe(agv);
                consumedMessage.Context.Message.AgvId.ShouldBe(agvId);
                orderDetails.OrderId.ShouldBe(orderId);
                orderDetails.Source.ShouldBe(fromStationId);

                // Get published MqttOrder Mqtt message
                IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

                // Check routing key and content type
                var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
                publishedMessage.Context.RoutingKey().ShouldBe(routingKeySpecified?.RoutingKeyValue);
                publishedMessage.Context.ContentType.ShouldBe(SystemTextJsonRawMessageSerializer.JsonContentType);

                // Check content
                var mqttMessage = publishedMessage.Context.Message;

                mqttMessage.SerialNumber.ShouldBe(agvId);
                mqttMessage.Stations?.FromStationIdentifier.ShouldBe(fromStationId);
                mqttMessage.Stations?.ToStationIdentifier.ShouldBe(toStationId);
                mqttMessage.OrderType.ShouldBe(OrderType.Transport);
                mqttMessage.RecordType.ShouldBe(OrderRecordType.Order);
            }
        }

        /// <summary>
        /// Test consuming an SendAgvChargeOrder message and publish an MqttOrder Mqtt message with proper bus configuration
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Test_Send_ChargeOrder_To_AGV_Via_MQTT()
        {
            var fixture = new MqttConsumerTestFixture<StartActionCommandConsumer>();

            // Create a publish observer
            var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
            using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
            {
                const string agvId = "agv0";
                const string chargerId = "CHG1";

                // Publish ExecuteTransportOrder
                await fixture.TestHarness.Bus.Publish(new SendAgvChargeOrder(Guid.NewGuid(), agvId, chargerId));

                // Check if sent
                (await fixture.TestHarness.Published.Any<SendAgvChargeOrder>()).ShouldBeTrue();

                // Get Consumer Harness
                IConsumerTestHarness<StartActionCommandConsumer> consumerHarness = fixture.TestHarness.GetConsumerHarness<StartActionCommandConsumer>();

                // Check if consumed
                (await consumerHarness.Consumed.Any<SendAgvChargeOrder>()).ShouldBeTrue();

                // Get consumed message
                IReceivedMessage<SendAgvChargeOrder> consumedMessage = consumerHarness.Consumed.Select<SendAgvChargeOrder>().First();

                // Check consumed message
                consumedMessage.Context.Message.AgvId.ShouldBe(agvId);
                consumedMessage.Context.Message.ToCharger.ShouldBe(chargerId);
                Guid? correlationId = consumedMessage.Context.CorrelationId;

                // Get published MqttOrder Mqtt message
                IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

                // Check routing key and content type
                var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
                publishedMessage.Context.RoutingKey().ShouldBe(routingKeySpecified?.RoutingKeyValue);
                publishedMessage.Context.ContentType.ShouldBe(SystemTextJsonRawMessageSerializer.JsonContentType);

                // Check content
                publishedMessage.Context.Message.SerialNumber.ShouldBe(agvId);
                publishedMessage.Context.Message.OrderId.ShouldBe(correlationId.ToString());
                publishedMessage.Context.Message.Stations?.ToStationIdentifier.ShouldBe(chargerId);
            }
        }
    }
}
