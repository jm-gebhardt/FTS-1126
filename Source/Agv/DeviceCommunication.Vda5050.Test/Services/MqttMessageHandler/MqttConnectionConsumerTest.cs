using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

[TestClass]
public class MqttConnectionConsumerTest
{
    [TestMethod]
    public async Task Consume_MqttConnection_Online_DoesNotPublishAgvLastWillReceived()
    {
        var fixture = new MqttConnectionConsumerTestFixture();

        var message = MqttConnectionGenerator.Generate(connectionState: MqttConnectionState.Online);
        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldNotHavePublishedAsync<AgvLastWillReceived>();
    }

    [DataRow(MqttConnectionState.Offline)]
    [DataRow(MqttConnectionState.ConnectionBroken)]
    [TestMethod]
    public async Task Consume_MqttConnection_NotOnline_PublishesAgvLastWillReceived(MqttConnectionState state)
    {
        var fixture = new MqttConnectionConsumerTestFixture();

        var message = MqttConnectionGenerator.Generate(connectionState: state);
        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AgvLastWillReceived>();
    }
}