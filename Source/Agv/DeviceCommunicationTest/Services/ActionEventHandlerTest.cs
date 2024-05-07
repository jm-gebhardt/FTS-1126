using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
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
        MqttConsumerTestFixture<ActionAbortedConsumer> fixture = new ActionEventConsumerMqttFixture()
            .WithAgvInformation(agvInformation);

        var actionCorrelationId = Guid.NewGuid();
        const string orderId = "1234";
        var orderCorrelationId = orderId.ToGuid();
        const string toStationId = "Drop5";

        var action = new AgvAction(actionCorrelationId, ActionType.Pick, toStationId, orderCorrelationId);
        var message = new ActionAborted(agvCorrelationId, action);

        await fixture.TestHarness.Bus.Publish(message);
        await fixture.ShouldHavePublishedAsync<MqttOrder>();
        await fixture.ShouldHaveConsumedAsync<ActionAborted>();
        var mqttMessage = await fixture.GetFirstPublishedMessageAsync<MqttOrder>();
        mqttMessage.SerialNumber.ShouldBe(agvInformation.AgvStatusReceived!.SerialNumber);
        mqttMessage.OrderType.ShouldBe(OrderType.Pick);
        mqttMessage.RecordType.ShouldBe(OrderRecordType.OrderCancel);
        mqttMessage.OrderId.ShouldBe(action.CorrelationId.ToString());
    }

    [TestMethod]
    public async Task Consume_ActionAborted_EmptyAgvId_DoesNotPublishMqttOrder()
    {
        MqttConsumerTestFixture<ActionAbortedConsumer> fixture = new ActionEventConsumerMqttFixture();

        var actionCorrelationId = Guid.NewGuid();
        const string orderId = "1234";
        var orderCorrelationId = orderId.ToGuid();
        const string toStationId = "Drop5";

        var action = new AgvAction(actionCorrelationId, ActionType.Pick, toStationId, orderCorrelationId);
        var correlationId = Guid.NewGuid();
        var message = new ActionAborted(correlationId, action);

        await fixture.TestHarness.Bus.Publish(message);
        await fixture.ShouldHaveConsumedAsync<ActionAborted>();
        await fixture.ShouldNotHavePublishedAsync<MqttOrder>();
    }
}