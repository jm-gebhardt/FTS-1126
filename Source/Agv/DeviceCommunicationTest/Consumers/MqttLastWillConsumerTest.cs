using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConnectionState = Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Enums.ConnectionState;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

[TestClass]
public class MqttLastWillConsumerTest
{
    [DataRow(ConnectionState.Offline)]
    [DataRow(ConnectionState.ConnectionBroken)]
    [DataTestMethod]
    public async Task Consume_MqttAgvLastWill_ValidData_PublishesAgvLastWillReceived(ConnectionState connectionState)
    {
        var fixture = new LastWillConsumerTestFixture();
        await fixture.TestHarness.Bus.PublishMqtt(new MqttAgvLastWill(1, DateTimeOffset.UtcNow, "version", "Agv01", "manufacturer", connectionState), null);

        await fixture.ShouldHaveConsumedAsync<MqttAgvLastWill>();
        await fixture.ShouldHavePublishedAsync<AgvLastWillReceived>();
    }

    [TestMethod]
    public async Task Consume_MqttAgvLastWill_ConnectionStateOnline_DoesNotPublishAgvLastWillReceived()
    {
        var fixture = new LastWillConsumerTestFixture();
        await fixture.TestHarness.Bus.PublishMqtt(new MqttAgvLastWill(1, DateTimeOffset.UtcNow, "version", "Agv01", "manufacturer", ConnectionState.Online), null);

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