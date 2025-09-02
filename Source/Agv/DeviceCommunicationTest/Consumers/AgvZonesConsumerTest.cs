using Gebhardt.Acc.Common.Messages.AgvManagement.Commands;
using Gebhardt.Acc.Common.Testing.Fixtures;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Domain.Services;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Gebhardt.Acc.DeviceCommunication.KarisAgv.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Gebhardt.Acc.DeviceCommunicationTest.Consumers;

[TestClass]
public class AgvZonesConsumerTest
{
    [TestMethod]
    public async Task Consume_RequestAgvZones_ValidData_PublishesMqttRequestZones()
    {
        var fixture = new AgvZonesConsumerTestFixture();

        await fixture.TestHarness.Bus.Publish(new RequestAgvZones(Guid.NewGuid(), "serial number"));

        await fixture.ShouldHavePublishedAsync<MqttRequestZones>();
    }

    [TestMethod]
    public async Task Consume_RemoveAgvFromZone_ValidData_PublishesMqttRequestZoneRemoveAgv()
    {
        var fixture = new AgvZonesConsumerTestFixture();

        await fixture.TestHarness.Bus.Publish(new RemoveAgvFromZone(Guid.NewGuid(), "serial number", "agv to remove", "zone id"));

        await fixture.ShouldHavePublishedAsync<MqttRequestZoneRemoveAgv>();
    }

    private class AgvZonesConsumerTestFixture : MassTransitConsumerTestFixture<AgvZonesConsumer>
    {
        private readonly Mock<IZoneRefreshStateService> _zonesRefreshStateService = new();

        public AgvZonesConsumerTestFixture()
        {
            AddServices(s => s.AddTransient(_ => _zonesRefreshStateService.Object));
            Build();
        }
    }
}