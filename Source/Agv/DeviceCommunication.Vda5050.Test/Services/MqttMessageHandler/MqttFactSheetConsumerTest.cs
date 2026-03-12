using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;
using Moq;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

[TestClass]
public class MqttFactSheetConsumerTest
{
    [TestMethod]
    public async Task Consume_MqttFactSheet_ValidData_PublishesAgvDataReceived()
    {
        var fixture = new MqttFactSheetConsumerTestFixture();

        var message = MqttFactSheetGenerator.Generate(actions: [MqttFactSheetGenerator.GenerateAgvAction()]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AgvDataReceived>();

        fixture.VerifyActionsUpdated(Times.Once());
        fixture.VerifyDataUpdated();
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_NoActions_PublishesAgvDataReceived()
    {
        var fixture = new MqttFactSheetConsumerTestFixture();

        var message = MqttFactSheetGenerator.Generate(actions:null);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AgvDataReceived>();

        fixture.VerifyActionsUpdated(Times.Never());
        fixture.VerifyDataUpdated();
        fixture.VerifyAgvUpdated(Times.Once());
    }

    [TestMethod]
    public async Task Consume_MqttFactSheet_InstantAction_PublishesAgvInstantActionDefinitionReceived()
    {
        var fixture = new MqttFactSheetConsumerTestFixture();

        var message = MqttFactSheetGenerator.Generate(actions: [MqttFactSheetGenerator.GenerateAgvAction("initToStation", MqttActionScope.Instant)]);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AgvDataReceived>();
        await fixture.ShouldHavePublishedAsync<AgvInstantActionDefinitionReceived>();

        fixture.VerifyActionsUpdated(Times.Once());
        fixture.VerifyDataUpdated();
        fixture.VerifyAgvUpdated(Times.Once());
    }
}