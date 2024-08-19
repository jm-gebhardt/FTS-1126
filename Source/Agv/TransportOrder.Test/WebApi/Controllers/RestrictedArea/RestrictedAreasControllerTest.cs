using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.RestrictedArea;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.RestrictedArea;

[TestClass]
public class RestrictedAreasControllerTest
{
    [TestMethod]
    public async Task GetRestrictedAras_ValidData_ReturnsRestrictedAreaDto()
    {
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(new List<PolygonPoint> { new(1, 1), new(1, 3), new(2, 2) });
        var fixture = new RestrictedAreasControllerTestFixture()
            .WithRestrictedArea(restrictedArea)
            .Build();

        ICollection<RestrictedAreaDto> restrictedAreaDto = await fixture.GetControllerResultValueAsync(c => c.GetRestrictedAreas([restrictedArea.Id]));

        restrictedAreaDto.Count.ShouldBe(1);
        restrictedAreaDto.First().Id.ShouldBe(restrictedArea.Id);
        restrictedAreaDto.First().Area.Count.ShouldBe(restrictedArea.Area.Count);
    }
}