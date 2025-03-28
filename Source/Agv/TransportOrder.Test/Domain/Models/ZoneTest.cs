using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class ZoneTest
{
    [TestMethod]
    public void Equals_EqualZones_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var zone = new Zone([new ZonePolygonPoint(1, 1, 0, id), new ZonePolygonPoint(2, 1, 1, id), new ZonePolygonPoint(2, 2, 2, id)], "zone", id, 1);
        var equalZoneOtherPolygonOrder = new Zone([ new ZonePolygonPoint(2, 1, 1, id),new ZonePolygonPoint(1, 1, 0, id), new ZonePolygonPoint(2, 2, 2, id)], "zone", id, 1);

        zone.Equals(equalZoneOtherPolygonOrder).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_ZoneIdDiffers_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var zone = new Zone([new ZonePolygonPoint(1, 1, 0, id), new ZonePolygonPoint(2, 1, 1, id), new ZonePolygonPoint(2, 2, 2, id)], "zone", id, 1);
        var differentId = Guid.NewGuid();
        var differenzZoneId = new Zone([new ZonePolygonPoint(2, 1, 1, differentId), new ZonePolygonPoint(1, 1, 0, differentId), new ZonePolygonPoint(2, 2, 2, differentId)], "zone", differentId, 1);

        zone.Equals(differenzZoneId).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_OccupancyDiffers_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var zone = new Zone([new ZonePolygonPoint(1, 1, 0, id), new ZonePolygonPoint(2, 1, 1, id), new ZonePolygonPoint(2, 2, 2, id)], "zone", id, 1);
        var differentOccupancy = new Zone([new ZonePolygonPoint(2, 1, 1, id), new ZonePolygonPoint(1, 1, 0, id), new ZonePolygonPoint(2, 2, 2, id)], "zone", id, 2);

        zone.Equals(differentOccupancy).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_DifferentPolygon_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var zone = new Zone([new ZonePolygonPoint(1, 1, 0, id), new ZonePolygonPoint(2, 1, 1, id), new ZonePolygonPoint(2, 2, 2, id)], "zone", id, 1);
        var differentPolygon = new Zone([new ZonePolygonPoint(1, 1, 0, id), new ZonePolygonPoint(2, 2, 1, id)], "zone", id, 1);

        zone.Equals(differentPolygon).ShouldBeFalse();
    }
}