using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Agv;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Helper.Mqtt;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Services.MqttMessageHandler;

[TestClass]
public class MqttStateConsumerTest
{
    [TestMethod]
    public async Task Consume_MqttState_ValidData_PublishesAgvStatusReceived_AgvPositionReceived_AgvErrorStatusReceived_AgvLoadStatusReceived()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var fixture = new MqttStateConsumerTestFixture().AddAgv(agv);

        var message = MqttStateGenerator.Generate(position: MqttVisualizationGenerator.GeneratePosition());

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AgvStatusReceived>();
        await fixture.ShouldHavePublishedAsync<AgvPositionReceived>();
        await fixture.ShouldHavePublishedAsync<AgvErrorStatusReceived>();
        await fixture.ShouldHavePublishedAsync<AgvLoadStatusReceived>();
    }

    [TestMethod]
    public async Task Consume_MqttState_NoPosition_DoesNotPublishAgvPositionReceived()
    {
        var agv = AgvGenerator.Generate(Guid.NewGuid());
        var fixture = new MqttStateConsumerTestFixture().AddAgv(agv);

        var message = MqttStateGenerator.Generate(position: null);

        await fixture.PublishMqttMessageAsync(message);

        await fixture.ShouldHavePublishedAsync<AgvStatusReceived>();
        await fixture.ShouldNotHavePublishedAsync<AgvPositionReceived>();
        await fixture.ShouldHavePublishedAsync<AgvErrorStatusReceived>();
        await fixture.ShouldHavePublishedAsync<AgvLoadStatusReceived>();
    }
}