using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Gebhardt.Acc.TransportOrder.Domain.Profiles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Profiles;

[TestClass]
public class AgvZoneProfileTest
{
    [TestMethod]
    public void Map_AgvZoneToZone_MapsAllPropertiesCorrectly()
    {
        var mapper = MappingHelper.GetMapper(new AgvZoneProfile());
        var polygonPoint = new PolygonPoint(1, 1);
        var agvZone = new AgvZone("zoneId", 1, [polygonPoint], ["agv"]);
        
        var zone = mapper.Map<Zone>(agvZone);

        zone.CorrelationId.ShouldBe(agvZone.ZoneId.ToGuid());
        zone.ZoneId.ShouldBe(agvZone.ZoneId);
        zone.Capacity.ShouldBe(agvZone.Capacity);
        zone.Polygon.Count.ShouldBe(agvZone.Polygon.Count);
        var zonePolygonPoint = zone.Polygon.First();
        zonePolygonPoint.LocationId.ShouldBe(zone.CorrelationId);
        zonePolygonPoint.X.ShouldBe(polygonPoint.X);
        zonePolygonPoint.Y.ShouldBe(polygonPoint.Y);
    }

    [TestMethod]
    public void Map_AgvZoneCollectionToZoneCollection_MapsAllPropertiesCorrectly()
    {
        var mapper = MappingHelper.GetMapper(new AgvZoneProfile());
        var polygonPoint = new PolygonPoint(1, 1);
        var agvZone = new AgvZone("zoneId", 1, [polygonPoint], ["agv"]);

        ICollection<AgvZone> agvZones = [agvZone];

        var zones = mapper.Map<ICollection<Zone>>(agvZones);

        zones.Count.ShouldBe(agvZones.Count);
        var zone = zones.First();
        zone.CorrelationId.ShouldBe(agvZone.ZoneId.ToGuid());
        zone.ZoneId.ShouldBe(agvZone.ZoneId);
        zone.Capacity.ShouldBe(agvZone.Capacity);
        zone.Polygon.Count.ShouldBe(agvZone.Polygon.Count);
        var zonePolygonPoint = zone.Polygon.First();
        zonePolygonPoint.LocationId.ShouldBe(zone.CorrelationId);
        zonePolygonPoint.X.ShouldBe(polygonPoint.X);
        zonePolygonPoint.Y.ShouldBe(polygonPoint.Y);
    }
}