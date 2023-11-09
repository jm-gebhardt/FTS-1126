using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using MassTransit.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Consumers;

/// <summary>
/// Test for OrderEventConsumer
/// </summary>
[TestClass]
public class OrderEventConsumerTest
{
    private const string SerialNumber = "AGV0";
    private const string OrderId = "ORDER123";
    private readonly Guid _actionGuid = OrderId.ToGuid();

    /// <summary>
    /// Test for consuming OrderEvent message with the event type 'Started'
    /// </summary>
    [TestMethod]
    public async Task ConsumeEventStarted()
    {
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, SerialNumber, OrderEventType.Started);
        var published = await ConsumeMessage(message);
        await AssertPublishedEvent<ActionStarted>(published);
    }

    /// <summary>
    /// Test for consuming OrderEvent message with the event type 'Completed'
    /// </summary>
    [TestMethod]
    public async Task ConsumeEventCompleted()
    {
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, SerialNumber, OrderEventType.Completed);
        var published = await ConsumeMessage(message);

        await AssertPublishedEvent<ActionCompleted>(published);
    }

    /// <summary>
    /// Test for consuming OrderEvent message with the event type 'Canceled'
    /// </summary>
    [TestMethod]
    public async Task ConsumeEventCanceled()
    {
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, SerialNumber, OrderEventType.Canceled);
        var published = await ConsumeMessage(message);

        await AssertPublishedEvent<ActionCanceled>(published);
    }

    /// <summary>
    /// Test for consuming OrderEvent message with the event type 'CancelFailed'
    /// </summary>
    [TestMethod]
    public async Task ConsumeEventCancelFailed()
    {
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, SerialNumber, OrderEventType.CancelFailed);
        var published = await ConsumeMessage(message);

        await AssertPublishedEvent<ActionCancelFailed>(published);
    }

    /// <summary>
    /// Test for consuming OrderEvent message with the event type 'Failed'
    /// </summary>
    [TestMethod]
    public async Task ConsumeEventFailed()
    {
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, SerialNumber, OrderEventType.Failed);
        var published = await ConsumeMessage(message);

        await AssertPublishedEvent<ActionFailed>(published);
    }

    private async Task<IPublishedMessageList> ConsumeMessage(MqttOrderEvent message)
    {
        var fixture = new MqttConsumerTestFixture<OrderEventConsumer>();
        await fixture.TestHarness.Bus.PublishMqtt(message, null);

        var consumedMsg = fixture.ConsumerHarness.Consumed.Select<MqttOrderEvent>().First().Context.Message;

        consumedMsg.SerialNumber.ShouldBe(message.SerialNumber);
        consumedMsg.OrderId.ShouldBe(message.OrderId);
        consumedMsg.OrderEventType.ShouldBe(message.OrderEventType);

        return fixture.TestHarness.Published;
    }

    private async Task AssertPublishedEvent<TMessage>(IPublishedMessageList published) where TMessage : class
    {
        (await published.Any<TMessage>()).ShouldBeTrue();

        IPublishedMessage<TMessage>? publishedEvent = await published.SelectAsync<TMessage>().First();
        var actionStatusEvent = publishedEvent.MessageObject as TMessage;
        actionStatusEvent.ShouldNotBeNull();
    }
}