using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.DeviceCommunication.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.Messages.MqttMessages;
using Gebhardt.Acc.DeviceCommunication.Services;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services;

[TestClass]
public class ActionEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionAborted_ValidData_PublishesMqttOrder()
    {
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        var agvCorrelationId = agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId;
        MqttConsumerTestFixture<ActionEventConsumer> fixture = new ActionEventConsumerMqttFixture()
            .WithAgvInformation(agvInformation);

        var actionCorrelationId = Guid.NewGuid();
        const string orderId = "1234";
        var orderCorrelationId = orderId.ToGuid();
        const string toStationId = "Drop5";

        var action = new AgvAction(actionCorrelationId, ActionType.Pick, toStationId, orderCorrelationId);
        var message = new ActionAborted(agvCorrelationId, action);

        await fixture.TestHarness.Bus.Publish(message);
        (await fixture.ConsumerHarness.Consumed.Any<ActionAborted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<MqttOrder>()).ShouldBeTrue();
        var mqttMessage = fixture.TestHarness.Published.Select<MqttOrder>().First().Context.Message;
        mqttMessage.SerialNumber.ShouldBe(agvInformation.AgvStatusReceived!.SerialNumber);
        mqttMessage.OrderType.ShouldBe(OrderType.Pick);
        mqttMessage.RecordType.ShouldBe(OrderRecordType.OrderCancel);
        mqttMessage.OrderId.ShouldBe(action.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_ActionAborted_EmptyAgvId_DoesNotPublishMqttOrder()
    {
        MqttConsumerTestFixture<ActionEventConsumer> fixture = new ActionEventConsumerMqttFixture();

        var actionCorrelationId = Guid.NewGuid();
        const string orderId = "1234";
        var orderCorrelationId = orderId.ToGuid();
        const string toStationId = "Drop5";

        var action = new AgvAction(actionCorrelationId, ActionType.Pick, toStationId, orderCorrelationId);
        var correlationId = Guid.NewGuid();
        var message = new ActionAborted(correlationId, action);

        await fixture.TestHarness.Bus.Publish(message);
        (await fixture.ConsumerHarness.Consumed.Any<ActionAborted>()).ShouldBeTrue();
        (await fixture.TestHarness.Published.Any<MqttOrder>()).ShouldBeFalse();
    }
}