using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;
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
    }
}