using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

[TestClass]
public class MqttVisualizationTest
{
    [TestMethod]
    public async Task Consume_MqttVisualization_ValidData_PublishesAgvPositionReceived()
    {
        var fixture = new MqttVisualizationConsumerTestFixture();

        var message = MqttVisualizationGenerator.Generate();

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AgvPositionReceived>();
    }
}