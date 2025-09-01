using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

[TestClass]
public class MqttZonesConsumerTest
{
    [TestMethod]
    public async Task Consume_MqttZones_EmptySerialNumber_DoesNotPublishesAgvZonesReceived()
    {
        var fixture = new MqttZonesConsumerTestFixture();

        var message = new MqttZones(string.Empty, "request id", [new MqttZone("zone id", 1, [new MqttPolygonPoint(1, 0, null), new MqttPolygonPoint(1, 1, null), new MqttPolygonPoint(0, 0, null)], [])]);
        
        await fixture.TestHarness.Bus.PublishMqtt(message, null);
        await fixture.ShouldHaveConsumedAsync<MqttZones>();
        await fixture.ShouldNotHavePublishedAsync<AgvZonesReceived>();
    }

    [TestMethod]
    public async Task Consume_MqttZones_ValidData_PublishAgvZonesReceived()
    {
        var fixture = new MqttZonesConsumerTestFixture();

        var message = new MqttZones("serial number", "request id", [new MqttZone("zone id", 1, [new MqttPolygonPoint(1, 0, null), new MqttPolygonPoint(1, 1, null), new MqttPolygonPoint(0, 0, null)], [])]);

        await fixture.TestHarness.Bus.PublishMqtt(message, null);
        await fixture.ShouldHaveConsumedAsync<MqttZones>();
        var publishedMessage = await fixture.GetFirstPublishedMessageAsync<AgvZonesReceived>();
        publishedMessage.CorrelationId.ShouldBe(message.SerialNumber.ToGuid());
        publishedMessage.SerialNumber.ShouldBe(message.SerialNumber);

        publishedMessage.AgvZones.Count.ShouldBe(message.Zones.Count);
        foreach (var zone in publishedMessage.AgvZones)
        {
            var mqttZone = message.Zones.Single(z => z.ZoneId == zone.ZoneId);
            zone.Capacity.ShouldBe(mqttZone.Capacity);
            zone.Occupancy.ShouldBeEquivalentTo(mqttZone.Occupancy);
            zone.Polygon.Count.ShouldBe(mqttZone.Polygon.Count);
        }
    }

    private class MqttZonesConsumerTestFixture : MqttConsumerTestFixture<MqttZonesConsumer>
    {
        public MqttZonesConsumerTestFixture()
        {
            Build();
        }
    }
}