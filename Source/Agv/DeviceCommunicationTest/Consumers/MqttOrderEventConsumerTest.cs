using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Extensions;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
using MassTransit.Testing;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

/// <summary>
/// Test for OrderEventConsumer
/// </summary>
[TestClass]
public class MqttOrderEventConsumerTest
{
    [TestMethod]
    public async Task ConsumeEventStarted_SameIds_UsesVehicleOrderId()
    {
        var fixture = new OrderEventConsumerFixture();
        var msg = MqttOrderEventGenerator.CreateMqttOrderEvent(OrderEventType.Started);

        var published = await fixture.PublishAndConsumeAsync(msg, shouldMappingBeNull: true);
        var action = await published.GetFirstPublishedMessageAsync<ActionStartedByAgv>();
        action.CorrelationId.ShouldBe(msg.OrderId.ToGuid());
    }

    [TestMethod]
    public async Task ConsumeEventStarted_DifferentIds_UsesInternalId()
    {
        var internalId = Guid.NewGuid();
        var msg = MqttOrderEventGenerator.CreateMqttOrderEvent(OrderEventType.Started);
        var fixture = new OrderEventConsumerFixture();
        await fixture.ActionRepository.AddVehicleAndInternalOrderIdMapping(msg.OrderId.ToGuid(), internalId);

        var published = await fixture.PublishAndConsumeAsync(msg, shouldMappingBeNull: false);
        var action = await published.GetFirstPublishedMessageAsync<ActionStartedByAgv>();
        action.CorrelationId.ShouldBe(internalId);
    }

    [TestMethod]
    public async Task ConsumeEventCompleted_SameIds_UsesVehicleOrderId()
    {
        var msg = MqttOrderEventGenerator.CreateMqttOrderEvent(OrderEventType.Completed);
        var fixture = new OrderEventConsumerFixture();

        var published = await fixture.PublishAndConsumeAsync(msg, shouldMappingBeNull: true);
        var action = await published.GetFirstPublishedMessageAsync<ActionCompletedByAgv>();
        action.CorrelationId.ShouldBe(msg.OrderId.ToGuid());
    }

    [TestMethod]
    public async Task ConsumeEventCompleted_DifferentIds_UsesInternalId()
    {
        var internalId = Guid.NewGuid();
        var msg = MqttOrderEventGenerator.CreateMqttOrderEvent(OrderEventType.Completed);
        var fixture = new OrderEventConsumerFixture();
        await fixture.ActionRepository.AddVehicleAndInternalOrderIdMapping(msg.OrderId.ToGuid(), internalId);

        var published = await fixture.PublishAndConsumeAsync(msg, shouldMappingBeNull: true);
        var action = await published.GetFirstPublishedMessageAsync<ActionCompletedByAgv>();
        action.CorrelationId.ShouldBe(internalId);
    }

    [TestMethod]
    public async Task ConsumeEventCanceled_SameIds_UsesVehicleOrderId()
    {
        var msg = MqttOrderEventGenerator.CreateMqttOrderEvent(OrderEventType.Canceled);
        var fixture = new OrderEventConsumerFixture();

        var published = await fixture.PublishAndConsumeAsync(msg, shouldMappingBeNull: true);
        var action = await published.GetFirstPublishedMessageAsync<ActionAbortedByAgv>();
        action.CorrelationId.ShouldBe(msg.OrderId.ToGuid());
    }

    [TestMethod]
    public async Task ConsumeEventCanceled_DifferentIds_UsesInternalId()
    {
        var internalId = Guid.NewGuid();
        var msg = MqttOrderEventGenerator.CreateMqttOrderEvent(OrderEventType.Canceled);
        var fixture = new OrderEventConsumerFixture();
        await fixture.ActionRepository.AddVehicleAndInternalOrderIdMapping(msg.OrderId.ToGuid(), internalId);

        var published = await fixture.PublishAndConsumeAsync(msg, shouldMappingBeNull: true);
        var action = await published.GetFirstPublishedMessageAsync<ActionAbortedByAgv>();
        action.CorrelationId.ShouldBe(internalId);
    }

    [TestMethod]
    public async Task ConsumeEventCancelFailed_SameIds_UsesVehicleOrderId()
    {
        var msg = MqttOrderEventGenerator.CreateMqttOrderEvent(OrderEventType.CancelFailed);
        var fixture = new OrderEventConsumerFixture();

        var published = await fixture.PublishAndConsumeAsync(msg, shouldMappingBeNull: true);
        var action = await published.GetFirstPublishedMessageAsync<ActionAbortionFailedByAgv>();
        action.CorrelationId.ShouldBe(msg.OrderId.ToGuid());
    }

    [TestMethod]
    public async Task ConsumeEventCancelFailed_DifferentIds_UsesInternalId()
    {
        var internalId = Guid.NewGuid();
        var msg = MqttOrderEventGenerator.CreateMqttOrderEvent(OrderEventType.CancelFailed);
        var fixture = new OrderEventConsumerFixture();
        await fixture.ActionRepository.AddVehicleAndInternalOrderIdMapping(msg.OrderId.ToGuid(), internalId);

        var published = await fixture.PublishAndConsumeAsync(msg, shouldMappingBeNull: false);
        var action = await published.GetFirstPublishedMessageAsync<ActionAbortionFailedByAgv>();
        action.CorrelationId.ShouldBe(internalId);
    }

    [TestMethod]
    public async Task ConsumeEventFailed_SameIds_UsesVehicleOrderId()
    {
        var msg = MqttOrderEventGenerator.CreateMqttOrderEvent(OrderEventType.Failed);
        var fixture = new OrderEventConsumerFixture();

        var published = await fixture.PublishAndConsumeAsync(msg, shouldMappingBeNull: true);
        var action = await published.GetFirstPublishedMessageAsync<ActionFailedByAgv>();
        action.CorrelationId.ShouldBe(msg.OrderId.ToGuid());
    }

    [TestMethod]
    public async Task ConsumeEventFailed_DifferentIds_UsesInternalId()
    {
        var internalId = Guid.NewGuid();
        var msg = MqttOrderEventGenerator.CreateMqttOrderEvent(OrderEventType.Failed);
        var fixture = new OrderEventConsumerFixture();
        await fixture.ActionRepository.AddVehicleAndInternalOrderIdMapping(msg.OrderId.ToGuid(), internalId);

        var published = await fixture.PublishAndConsumeAsync(msg, shouldMappingBeNull: true);
        var action = await published.GetFirstPublishedMessageAsync<ActionFailedByAgv>();
        action.CorrelationId.ShouldBe(internalId);
    }
}

internal class OrderEventConsumerFixture : ActionRepositoryConsumerTestFixture<MqttOrderEventConsumer>
{
    public OrderEventConsumerFixture()
    {
        Build();
    }

    public async Task<IPublishedMessageList> PublishAndConsumeAsync(MqttOrderEvent message, bool shouldMappingBeNull)
    {
        await TestHarness.Bus.PublishMqtt(message, callback: null);
        await ShouldHaveConsumedAsync<MqttOrderEvent>();

        var consumed = await GetFirstConsumedMessageAsync<MqttOrderEvent>();
        consumed.SerialNumber.ShouldBe(message.SerialNumber);
        consumed.OrderId.ShouldBe(message.OrderId);
        consumed.OrderEventType.ShouldBe(message.OrderEventType);

        if (shouldMappingBeNull)
        {
            (await ActionRepository.GetInternalOrderId(message.OrderId.ToGuid())).ShouldBeNull();
        }
        else
        {
            (await ActionRepository.GetInternalOrderId(message.OrderId.ToGuid())).ShouldNotBeNull();
        }

        return TestHarness.Published;
    }
}