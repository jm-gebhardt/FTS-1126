using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using ConnectionState = Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums.ConnectionState;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

[TestClass]
public class MqttLastWillConsumerTest
{
    [DataRow(ConnectionState.Offline)]
    [DataRow(ConnectionState.ConnectionBroken)]
    [TestMethod]
    public async Task Consume_MqttAgvLastWill_ValidData_PublishesAgvLastWillReceived(ConnectionState connectionState)
    {
        var fixture = new LastWillConsumerTestFixture();
        await fixture.TestHarness.Bus.PublishMqtt(new MqttAgvLastWill(1, DateTimeOffset.UtcNow, "version", "Agv01", "manufacturer", connectionState), callback: null);

        await fixture.ShouldHaveConsumedAsync<MqttAgvLastWill>();
        await fixture.ShouldHavePublishedAsync<AgvLastWillReceived>();
    }

    [TestMethod]
    public async Task Consume_MqttAgvLastWill_ConnectionStateOnline_DoesNotPublishAgvLastWillReceived()
    {
        var fixture = new LastWillConsumerTestFixture();
        await fixture.TestHarness.Bus.PublishMqtt(new MqttAgvLastWill(1, DateTimeOffset.UtcNow, "version", "Agv01", "manufacturer", ConnectionState.Online), callback: null);

        await fixture.ShouldHaveConsumedAsync<MqttAgvLastWill>();
        await fixture.ShouldNotHavePublishedAsync<AgvLastWillReceived>();
    }
}

internal class LastWillConsumerTestFixture : MqttConsumerTestFixture<MqttLastWillConsumer>
{
    public LastWillConsumerTestFixture()
    {
        Build();
    }
}