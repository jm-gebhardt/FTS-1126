using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Extensions;
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
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
        /// Test consuming an ExecuteOrder message and publish an MqttOrder Mqtt message with proper bus configuration
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Test_Send_TransportOrder_To_AGV_Via_MQTT()
        {
            MqttConsumerTestFixture<StartActionCommandConsumer> fixture = new MqttConsumerTestFixture<StartActionCommandConsumer>()
                .AddServices(s=>s.AddSingleton<IAgvRegistry>(new Mock<AgvRegistry>().Object))
                .Build();

            // Create a publish observer
            var publishObserver = new BusTestPublishObserver(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
            using (fixture.TestHarness.Bus.ConnectPublishObserver(publishObserver))
            {
                var actionCorrelationId = Guid.NewGuid();
                const string agvId = "agv0";
                const string orderId = "1234";
                var orderCorrelationId = orderId.ToGuid();
                const string fromStationId = "Pick1";
                const string toStationId = "Drop5";

                var action = new AgvAction(actionCorrelationId, agvId, ActionType.Pick, orderId, orderCorrelationId, fromStationId, toStationId);
                var message = new StartActionCommand(Guid.NewGuid(), action);

                // Publish ExecuteTransportOrder
                await fixture.TestHarness.Bus.Publish(message);

                // Get Consumer Harness
                IConsumerTestHarness<StartActionCommandConsumer> consumerHarness = fixture.TestHarness.GetConsumerHarness<StartActionCommandConsumer>();

                // Check if consumed
                (await consumerHarness.Consumed.Any<StartActionCommand>()).ShouldBeTrue();

                // Get consumed message
                IReceivedMessage<StartActionCommand> consumedMessage = consumerHarness.Consumed.Select<StartActionCommand>().First();
                action = consumedMessage.Context.Message.Action;

                // Check consumed message
                action.CorrelationId.ShouldBe(actionCorrelationId);
                action.AgvId.ShouldBe(agvId);
                action.OrderId.ShouldBe(orderId);
                action.ParentId.ShouldBe(orderCorrelationId);
                action.Source.ShouldBe(fromStationId);
                action.Destination.ShouldBe(toStationId);

                // Get published MqttOrder Mqtt message
                IPublishedMessage<MqttOrder> publishedMessage = publishObserver.Messages.Select<MqttOrder>().First();

                // Check routing key and content type
                var routingKeySpecified = typeof(MqttOrder).GetCustomAttribute(typeof(RoutingKey)) as RoutingKey;
                publishedMessage.Context.RoutingKey().ShouldBe(routingKeySpecified?.RoutingKeyValue);
                publishedMessage.Context.ContentType.ShouldBe(SystemTextJsonRawMessageSerializer.JsonContentType);

                // Check content
                var mqttMessage = publishedMessage.Context.Message;

                mqttMessage.SerialNumber.ShouldBe(agvId);
                // For pick action Karis expects destination as source ...
                mqttMessage.Stations?.FromStationIdentifier.ShouldBe(toStationId);
                // ... and empty destination.
                mqttMessage.Stations?.ToStationIdentifier.ShouldBe(string.Empty);
                mqttMessage.OrderType.ShouldBe(OrderType.Pick);
                mqttMessage.RecordType.ShouldBe(OrderRecordType.Order);

                // ... and action Guid with parentId
                string actionId = mqttMessage.OrderId.Split(" ")[0];
                string parentId = mqttMessage.OrderId.Split(" ")[1];
                actionId.ShouldBe(actionCorrelationId.ToString());
                parentId.ShouldBe(orderId);
            }
        }
    }
}
