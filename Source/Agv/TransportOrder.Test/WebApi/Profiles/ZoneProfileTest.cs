using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Zone;
using Gebhardt.Acc.TransportOrder.WebApi.Profiles;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Profiles;

[TestClass]
public class ZoneProfileTest
{
    [TestMethod]
    public void Map_ZoneToZoneDto_MapsAllPropertiesCorrectly()
    {
        var mapper = MappingHelper.GetMapper(new ZoneProfile());
        var zone = new Zone([], "zoneId", "zoneId".ToGuid(), 4);

        var zoneDto = mapper.Map<ZoneDto>(zone);

        zoneDto.CorrelationId.ShouldBe(zone.CorrelationId);
        zoneDto.Capacity.ShouldBe(zone.Capacity);
        zoneDto.ZoneId.ShouldBe(zone.ZoneId);
        zoneDto.Polygon.ShouldBeEmpty();
    }

    [TestMethod]
    public void Map_ZonePolygonPointToZonePolygonPointDto_MapsAllPropertiesCorrectly()
    {
        var mapper = MappingHelper.GetMapper(new ZoneProfile());
        var point = new ZonePolygonPoint(1d, 2d, 0, "polygon".ToGuid());

        var pointDto = mapper.Map<ZonePolygonPointDto>(point);

        pointDto.Index.ShouldBe(point.Index);
        pointDto.LocationId.ShouldBe(point.LocationId);
        pointDto.X.ShouldBeInRange(point.X - Mathematics.DoubleComparator, point.X + Mathematics.DoubleComparator);
        pointDto.Y.ShouldBeInRange(point.Y - Mathematics.DoubleComparator, point.Y + Mathematics.DoubleComparator);
    }
}