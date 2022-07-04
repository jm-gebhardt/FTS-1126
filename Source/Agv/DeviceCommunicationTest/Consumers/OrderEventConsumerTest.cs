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
        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Started'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventStarted()
        {
            var published = await ConsumeMessage(OrderEventType.Started);
            Assert.IsTrue(await published.Any<ITransportOrderStarted>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Picked'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventPicked()
        {
            var published = await ConsumeMessage(OrderEventType.Picked);
            Assert.IsTrue(await published.Any<ITransportOrderPickCompleted>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Completed'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventCompleted()
        {
            var published = await ConsumeMessage(OrderEventType.Completed);
            Assert.IsTrue(await published.Any<ITransportOrderCompleted>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Cancelled'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventCancelled()
        {
            var published = await ConsumeMessage(OrderEventType.Cancelled);
            Assert.IsTrue(await published.Any<ITransportOrderCancelSucceeded>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'CancellFailed'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventCancellFailed()
        {
            var published = await ConsumeMessage(OrderEventType.CancellFailed);
            Assert.IsTrue(await published.Any<ITransportOrderCancelFailed>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Failed'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventFailed()
        {
            var published = await ConsumeMessage(OrderEventType.Failed);
            Assert.IsTrue(await published.Any<ITransportOrderFailed>());
        }

        /// <summary>
        /// Test for consuming OrderEvent message with an undefined event type
        /// </summary>
        [TestMethod]
        public async Task ConsumeUndefined()
        {
            var published = await ConsumeMessage(null);
            Thread.Sleep(1); // Wait 1ms before checking if message was published
            Assert.IsFalse(await published.Any<ITransportOrderFailed>());
        }

        private async Task<IPublishedMessageList> ConsumeMessage(OrderEventType? eventType)
        {
            const string serialNumber = "AGV0";
            const string orderId = "ORDER123";

            var fixture = new MqttConsumerTestFixture<OrderEventConsumer>();
            await fixture.TestHarness.Bus.PublishMqtt<IOrderEvent>(new {
                SerialNumber = serialNumber,
                OrderId = orderId,
                EventType = eventType
            }, null);

            var consumedMsg = fixture.ConsumerHarness.Consumed.Select<IOrderEvent>().First().Context.Message;

            Assert.AreEqual(serialNumber, consumedMsg.SerialNumber);
            Assert.AreEqual(orderId, consumedMsg.OrderId);
            Assert.AreEqual(eventType, consumedMsg.EventType);

            return fixture.TestHarness.Published;
        }
    }
}
