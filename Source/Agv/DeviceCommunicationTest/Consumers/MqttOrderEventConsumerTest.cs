using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Repositories;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

/// <summary>
/// Test for OrderEventConsumer
/// </summary>
[TestClass]
public class MqttOrderEventConsumerTest
{
    private static string GetUniqueAgvSerialNumber() => $"#AGV-{Guid.NewGuid()}";

    private const string OrderId = "ORDER123";
    private readonly Guid _actionGuid = OrderId.ToGuid();

    /// <summary>
    /// Test for consuming OrderEvent message with the event type 'Started'
    /// </summary>
    [TestMethod]
    public async Task ConsumeEventStarted_SameInternalAndVehicleOrderId_CorrectCorrelationId()
    {
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, GetUniqueAgvSerialNumber(), OrderEventType.Started);
        var published = await ConsumeMessage(message, null, false);
        var publishedMessage = await AssertPublishedEvent<ActionStarted>(published);
        publishedMessage.CorrelationId.ShouldBe(message.OrderId.ToGuid());
    }

    [TestMethod]
    public async Task ConsumeEventStarted_InternalAndVehicleOrderIdDiffer_CorrectCorrelationId()
    {
        var internalOrderId = Guid.NewGuid();
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, GetUniqueAgvSerialNumber(), OrderEventType.Started);
        var published = await ConsumeMessage(message, internalOrderId, false);
        var publishedMessage = await AssertPublishedEvent<ActionStarted>(published);
        publishedMessage.CorrelationId.ShouldBe(internalOrderId);
    }

    /// <summary>
    /// Test for consuming OrderEvent message with the event type 'Completed'
    /// </summary>
    [TestMethod]
    public async Task ConsumeEventCompleted_SameInternalAndVehicleOrderId_CorrectCorrelationId()
    {
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, GetUniqueAgvSerialNumber(), OrderEventType.Completed);
        var published = await ConsumeMessage(message, null, false);

        var publishedMessage = await AssertPublishedEvent<ActionCompleted>(published);
        publishedMessage.CorrelationId.ShouldBe(message.OrderId.ToGuid());
    }

    [TestMethod]
    public async Task ConsumeEventCompleted_InternalAndVehicleOrderIdDiffer_CorrectCorrelationId()
    {
        var internalOrderId = Guid.NewGuid();
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, GetUniqueAgvSerialNumber(), OrderEventType.Completed);
        var published = await ConsumeMessage(message, internalOrderId, true);

        var publishedMessage = await AssertPublishedEvent<ActionCompleted>(published);
        publishedMessage.CorrelationId.ShouldBe(internalOrderId);
    }

    /// <summary>
    /// Test for consuming OrderEvent message with the event type 'Canceled'
    /// </summary>
    [TestMethod]
    public async Task ConsumeEventCanceled_SameInternalAndVehicleOrderId_CorrectCorrelationId()
    {
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, GetUniqueAgvSerialNumber(), OrderEventType.Canceled);
        var published = await ConsumeMessage(message, null, false);

        var publishedMessage = await AssertPublishedEvent<ActionCanceled>(published);
        publishedMessage.CorrelationId.ShouldBe(message.OrderId.ToGuid());
    }

    [TestMethod]
    public async Task ConsumeEventCanceled_InternalAndVehicleOrderIdDiffer_CorrectCorrelationId()
    {
        var internalOrderId = Guid.NewGuid();
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, GetUniqueAgvSerialNumber(), OrderEventType.Canceled);
        var published = await ConsumeMessage(message, internalOrderId, true);

        var publishedMessage = await AssertPublishedEvent<ActionCanceled>(published);
        publishedMessage.CorrelationId.ShouldBe(internalOrderId);
    }

    /// <summary>
    /// Test for consuming OrderEvent message with the event type 'CancelFailed'
    /// </summary>
    [TestMethod]
    public async Task ConsumeEventCancelFailed_SameInternalAndVehicleOrderId_CorrectCorrelationId()
    {
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, GetUniqueAgvSerialNumber(), OrderEventType.CancelFailed);
        var published = await ConsumeMessage(message, null, false);

        var publishedMessage = await AssertPublishedEvent<ActionCancelFailed>(published);
        publishedMessage.CorrelationId.ShouldBe(message.OrderId.ToGuid());
    }

    [TestMethod]
    public async Task ConsumeEventCancelFailed_InternalAndVehicleOrderIdDiffer_CorrectCorrelationId()
    {
        var internalOrderId = Guid.NewGuid();
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, GetUniqueAgvSerialNumber(), OrderEventType.CancelFailed);
        var published = await ConsumeMessage(message, internalOrderId, false);

        var publishedMessage = await AssertPublishedEvent<ActionCancelFailed>(published);
        publishedMessage.CorrelationId.ShouldBe(internalOrderId);
    }

    /// <summary>
    /// Test for consuming OrderEvent message with the event type 'Failed'
    /// </summary>
    [TestMethod]
    public async Task ConsumeEventFailed_SameInternalAndVehicleOrderId_CorrectCorrelationId()
    {
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, GetUniqueAgvSerialNumber(), OrderEventType.Failed);
        var published = await ConsumeMessage(message, null, false);

        var publishedMessage = await AssertPublishedEvent<ActionFailed>(published);
        publishedMessage.CorrelationId.ShouldBe(message.OrderId.ToGuid());
    }

    [TestMethod]
    public async Task ConsumeEventFailed_InternalAndVehicleOrderIdDiffer_CorrectCorrelationId()
    {
        var internalOrderId = Guid.NewGuid();
        var message = new MqttOrderEvent(_actionGuid + " " + OrderId, GetUniqueAgvSerialNumber(), OrderEventType.Failed);
        var published = await ConsumeMessage(message, internalOrderId, true);

        var publishedMessage = await AssertPublishedEvent<ActionFailed>(published);
        publishedMessage.CorrelationId.ShouldBe(internalOrderId);
    }

    private async Task<IPublishedMessageList> ConsumeMessage(MqttOrderEvent message, Guid? internalOrderId, bool deleted)
    {
        var fixture = new OrderEventConsumerFixture().WithInternalOrderId(internalOrderId);
        await fixture.TestHarness.Bus.PublishMqtt(message, null);

        var consumedMsg = await fixture.GetFirstConsumedMessageAsync<MqttOrderEvent>();

        consumedMsg.SerialNumber.ShouldBe(message.SerialNumber);
        consumedMsg.OrderId.ShouldBe(message.OrderId);
        consumedMsg.OrderEventType.ShouldBe(message.OrderEventType);

        fixture.VerifyOrderMappingRemoved(deleted ? Times.Once() : Times.Never());
        return fixture.TestHarness.Published;
    }

    private async Task<TMessage> AssertPublishedEvent<TMessage>(IPublishedMessageList published) where TMessage : class
    {
        (await published.Any<TMessage>()).ShouldBeTrue();

        IPublishedMessage<TMessage>? publishedEvent = await published.SelectAsync<TMessage>().First();
        var actionStatusEvent = publishedEvent.MessageObject as TMessage;
        actionStatusEvent.ShouldNotBeNull();
        return publishedEvent.Context.Message;
    }
}

internal class OrderEventConsumerFixture : MqttConsumerTestFixture<MqttOrderEventConsumer>
{
    private readonly Mock<IActionRepository> _actionRepository = new();

    public OrderEventConsumerFixture()
    {
        AddServices(s => s.AddTransient(_ => _actionRepository.Object));
        _actionRepository.Setup(x => x.RemoveVehicleAndInternalOrderIdMapping(It.IsAny<Guid>())).Verifiable();
        Build();
    }

    public OrderEventConsumerFixture WithInternalOrderId(Guid? correlationId)
    {
        _actionRepository.Setup(x => x.GetInternalOrderId(It.IsAny<Guid>())).ReturnsAsync(correlationId);
        return this;
    }

    public void VerifyOrderMappingRemoved(Times times)
    {
        _actionRepository.Verify(x => x.RemoveVehicleAndInternalOrderIdMapping(It.IsAny<Guid>()), times);
    }
}