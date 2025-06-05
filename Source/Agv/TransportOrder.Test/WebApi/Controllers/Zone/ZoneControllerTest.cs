using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Zone;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.Zone;

[TestClass]
public class ZoneControllerTest
{
    [TestMethod]
    public async Task GetZones_ValidData_GotZones()
    {
        var fixture = new ZoneControllerTestFixture().WithZones([new TransportOrder.Domain.Models.Zone.Zone([], "zoneId", "zoneId".ToGuid(), 5)]).Build();

        ICollection<ZoneDto> zones = await fixture.GetControllerResultValueAsync(c => c.GetZones());

        zones.Count.ShouldBe(1);
        zones.First().CorrelationId.ShouldBe("zoneId".ToGuid());
        zones.First().ZoneId.ShouldBe("zoneId");
        zones.First().Capacity.ShouldBe(5);
        zones.First().Polygon.ShouldBeEmpty();
    }
}