using Microsoft.VisualStudio.TestTools.UnitTesting;
using MassTransit.Testing;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.Actions.Events;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.Common.Messages.TransportOrder.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using MassTransit;

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
            await AssertPublishedEvent(published, ActionStatusChangedType.Started);
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Picked'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventPicked()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.Picked);
            var published = await ConsumeMessage(message);

            await AssertPublishedEvent(published, ActionStatusChangedType.Picked);
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Completed'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventCompleted()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.Completed);
            var published = await ConsumeMessage(message);

            await AssertPublishedEvent(published, ActionStatusChangedType.Completed);
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Cancelled'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventCancelled()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.Cancelled);
            var published = await ConsumeMessage(message);

            await AssertPublishedEvent(published, ActionStatusChangedType.Cancelled);
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'CancelFailed'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventCancelFailed()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.CancelFailed);
            var published = await ConsumeMessage(message);

            await AssertPublishedEvent(published, ActionStatusChangedType.CancelFailed);
        }

        /// <summary>
        /// Test for consuming OrderEvent message with the event type 'Failed'
        /// </summary>
        [TestMethod]
        public async Task ConsumeEventFailed()
        {
            var message = new MqttOrderEvent(SerialNumber, OrderId, OrderEventType.Failed);
            var published = await ConsumeMessage(message);
            
            await AssertPublishedEvent(published, ActionStatusChangedType.Failed);
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
            Assert.AreEqual(message.OrderEventType, consumedMsg.OrderEventType);

            return fixture.TestHarness.Published;
        }

        private async Task AssertPublishedEvent(IPublishedMessageList published, ActionStatusChangedType changedType)
        {
            Assert.IsTrue(await published.Any<ActionStatusChangedEvent>());

            IPublishedMessage<ActionStatusChangedEvent>? publishedEvent = await published.SelectAsync<ActionStatusChangedEvent>().First();
            var actionStatusEvent = publishedEvent.MessageObject as ActionStatusChangedEvent;
            Assert.AreEqual(actionStatusEvent!.ChangedType, changedType);
        }
    }
}
