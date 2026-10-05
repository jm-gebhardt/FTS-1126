using Gebhardt.Acc.DeviceCommunication.KarisAgv.Messages.Mqtt;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Domain.Services;

[TestClass]
public class ZoneRefreshStateServiceTest
{
    [TestMethod]
    public async Task AddOfflineAgvInZones_ValidData_HasOfflineAgvs()
    {
        var fixture = new ZoneRefreshStateServiceTestFixture();

        const string onlineAgv = "online";
        var offlineAgv = Guid.NewGuid();
        const string zone = "zone";
        const string serialNumber = "serial number";
        
        await fixture.Service.AddOfflineAgvInZones(offlineAgv, serialNumber, [zone], [onlineAgv]);

        fixture.Service.GetNextOnlineAgv().ShouldBe(onlineAgv);
        fixture.Service.OfflineAgvsInZones.Count.ShouldBe(1);
        fixture.Service.OfflineAgvsInZones.First().AgvCorrelationId.ShouldBe(offlineAgv);
        fixture.Service.OfflineAgvsInZones.First().CurrentZones.Single().ShouldBe(zone);
        fixture.Service.OfflineAgvsInZones.First().SerialNumber.ShouldBe(serialNumber);
        fixture.Service.OfflineAgvsInZones.First().IsInAnyZone.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SetAgvBackOnline_ValidData_RemovesAgvFromOfflineAgvs()
    {
        var fixture = new ZoneRefreshStateServiceTestFixture();

        const string onlineAgv = "online";
        var offlineAgv = Guid.NewGuid();
        const string zone = "zone";
        const string serialNumber = "serial number";
        await fixture.Service.AddOfflineAgvInZones(offlineAgv, serialNumber, [zone], [onlineAgv]);

        await fixture.Service.SetAgvBackOnline(offlineAgv);

        fixture.Service.OfflineAgvsInZones.Count.ShouldBe(0);
    }

    [TestMethod]
    public async Task SetAgvBackOnline_UnknownAgv_DoesNotRemoveFromOfflineAgvs()
    {
        var fixture = new ZoneRefreshStateServiceTestFixture();

        const string onlineAgv = "online";
        var offlineAgv = Guid.NewGuid();
        const string zone = "zone";
        const string serialNumber = "serial number";
        await fixture.Service.AddOfflineAgvInZones(offlineAgv, serialNumber, [zone], [onlineAgv]);
        fixture.Service.OfflineAgvsInZones.Count.ShouldBe(1);

        await fixture.Service.SetAgvBackOnline(Guid.NewGuid());

        fixture.Service.OfflineAgvsInZones.Count.ShouldBe(1);
    }

    [TestMethod]
    public async Task UpdateAgvsFromRefreshedZones_RemovedFromAllZones_ReturnsRemovedZones()
    {
        var fixture = new ZoneRefreshStateServiceTestFixture();

        const string onlineAgv = "online";
        var offlineAgv = Guid.NewGuid();
        List<string> zones = ["charger1", "charger3", "charger5"];
        const string serialNumber = "serial number";
        await fixture.Service.AddOfflineAgvInZones(offlineAgv, serialNumber, zones, [onlineAgv]);
        fixture.Service.OfflineAgvsInZones.Count.ShouldBe(1);

        var mqttZones = zones.Select(z => new MqttZone(z, 0, [], [])).ToList();

        var removedZonesByAgv = await fixture.Service.UpdateAgvsFromRefreshedZones(mqttZones);

        removedZonesByAgv.Count.ShouldBe(1);
        removedZonesByAgv[offlineAgv].Count.ShouldBe(zones.Count);
        removedZonesByAgv[offlineAgv].ShouldBe(zones, ignoreOrder:true);

        fixture.Service.OfflineAgvsInZones.Count.ShouldBe(0);
    }
}