using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups.Events;
using Gebhardt.StoreWare.Agv.Common.Testing;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Enums;
using Gebhardt.StoreWare.Agv.DeviceCommunication.MqttMessages;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services;

[TestClass]
public class ActionEventHandlerTest
{
    [TestMethod]
    public async Task Consume_ActionAborted_ValidData_PublishesMqttOrder()
    {
        var agvInformation = AgvInformationGenerator.GetAgvInformation();
        var agvCorrelationId = agvInformation.AgvStatusReceived!.AgvStatus.CorrelationId;
        var agvRepositoryTestFixture = new AgvRepositoryTestFixture()
            .WithAgvInformation(agvInformation);
        MqttConsumerTestFixture<ActionEventConsumer> fixture = new MqttConsumerTestFixture<ActionEventConsumer>(
                services: s => s.AddTransient(_ => agvRepositoryTestFixture.AgvRepository))
            .Build();
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
        MqttConsumerTestFixture<ActionEventConsumer> fixture = new MqttConsumerTestFixture<ActionEventConsumer>(
                services: s => s.AddTransient(_ => new AgvRepositoryTestFixture().AgvRepository))
            .Build();

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