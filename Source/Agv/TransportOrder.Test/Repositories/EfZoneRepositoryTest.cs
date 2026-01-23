using Gebhardt.Acc.TransportOrder.Domain.Models.Zone;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfZoneRepositoryTest
{
    [TestMethod]
    public async Task AddOrUpdateZone_UpdateWithIdenticalZone_ZoneUnchanged()
    {
        var fixture = new EfZoneRepositoryTestFixture();

        var id = Guid.NewGuid();
        var zone = new Zone([new ZonePolygonPoint(1, 1, 0, id), new ZonePolygonPoint(2, 1, 1, id), new ZonePolygonPoint(2, 2, 2, id)], "zone", id, 1);

        await fixture.Repository.AddOrUpdateZone(zone);
        (await fixture.Repository.GetZones([zone.ZoneName])).Single().ShouldBeEquivalentTo(zone);

        await fixture.Repository.AddOrUpdateZone(zone);
        (await fixture.Repository.GetZones([zone.ZoneName])).Single().ShouldBeEquivalentTo(zone);
    }
}