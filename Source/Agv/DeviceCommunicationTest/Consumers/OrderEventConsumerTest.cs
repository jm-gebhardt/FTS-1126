using Microsoft.VisualStudio.TestTools.UnitTesting;
using MassTransit.Testing;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers
{
    /// <summary>
    /// Test for OrderEventConsumer
    /// </summary>
    [TestClass]
    public class OrderEventConsumerTest
    {
        private const string SerialNumber = "AGV0";
        private const string OrderId = "ORDER123";

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Started'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventStarted()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.Started);
            var published = await ConsumeMessage(message);
            Assert.IsTrue(await published.Any<TransportOrderStarted>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Picked'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventPicked()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.Picked);
            var published = await ConsumeMessage(message);
            Assert.IsTrue(await published.Any<TransportOrderPickCompleted>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Completed'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventCompleted()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.Completed);
            var published = await ConsumeMessage(message);
            Assert.IsTrue(await published.Any<TransportOrderCompleted>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Cancelled'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventCancelled()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.Cancelled);
            var published = await ConsumeMessage(message);
            Assert.IsTrue(await published.Any<TransportOrderCancelSucceeded>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'CancellFailed'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventCancelFailed()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.CancellFailed);
            var published = await ConsumeMessage(message);
            Assert.IsTrue(await published.Any<TransportOrderCancelFailed>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Failed'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventFailed()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.Failed);
            var published = await ConsumeMessage(message);
            Assert.IsTrue(await published.Any<TransportOrderFailed>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with an undefined event type
        /// </summary>
        [TestMethod]
        public async Task ConsumeUndefined()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, null);
            var published = await ConsumeMessage(message);
            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await published.Any<TransportOrderFailed>());
        }

        private async Task<IPublishedMessageList> ConsumeMessage(MqttOrderEvent message)
        {
            var fixture = new MqttConsumerTestFixture<OrderEventConsumer>();
            await fixture.TestHarness.Bus.PublishMqtt(message, null);

            var consumedMsg = fixture.ConsumerHarness.Consumed.Select<MqttOrderEvent>().First().Context.Message;

            Assert.AreEqual(message.SerialNumber, consumedMsg.SerialNumber);
            Assert.AreEqual(message.OrderId, consumedMsg.OrderId);
            Assert.AreEqual(message.EventType, consumedMsg.EventType);

            return fixture.TestHarness.Published;
        }
    }
}
