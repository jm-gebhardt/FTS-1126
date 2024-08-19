using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Trigger;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.WebApi.Controllers.RestrictedArea;

[TestClass]
public class RestrictedAreaControllerTest
{
    [TestMethod]
    public async Task GetRestrictedAra_ValidData_ReturnsRestrictedAreaDto()
    {
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(new List<PolygonPoint> { new(1, 1), new(1, 3), new(2, 2) });
        var fixture = new RestrictedAreaControllerTestFixture()
            .WithRestrictedArea(restrictedArea)
            .Build();

        var restrictedAreaDto = await fixture.GetControllerResultValueAsync(c => c.GetRestrictedArea(restrictedArea.Id));

        restrictedAreaDto.Id.ShouldBe(restrictedArea.Id);
        restrictedAreaDto.Area.Count.ShouldBe(restrictedArea.Area.Count);
    }

    [TestMethod]
    public async Task AddRestrictedArea_ValidData_ReturnsRestrictedAreaDto()
    {
        var polygon = new List<PolygonPoint> { new(1, 1), new(1, 3), new(2, 2) };
        var restrictedArea = new TransportOrder.Domain.Models.RestrictedArea.RestrictedArea(polygon);

        var fixture = new RestrictedAreaControllerTestFixture()
            .WithRestrictedArea(restrictedArea)
            .Build();

        var restrictedAreaDto = await fixture.GetControllerResultValueAsync(c => c.AddRestrictedArea(fixture.Mapper.Map<List<PolygonPointDto>>(polygon)));

        restrictedAreaDto.Id.ShouldBe(restrictedArea.Id);
        restrictedAreaDto.Area.Count.ShouldBe(restrictedArea.Area.Count);
    }

    [TestMethod]
    public async Task UpdateRestrictedArea_ValidData_Returns()
    {
        var polygon = new List<PolygonPoint> { new(1, 1), new(1, 3), new(2, 2) };
        var fixture = new RestrictedAreaControllerTestFixture()
            .Build();

        await fixture.GetControllerResultAsync<ActionResult>(c => c.UpdateRestrictedArea(Guid.NewGuid(), fixture.Mapper.Map<List<PolygonPointDto>>(polygon)));
    }

    [TestMethod]
    public async Task DeleteRestrictedArea_ValidData_Returns()
    {
        var fixture = new RestrictedAreaControllerTestFixture()
            .Build();

        await fixture.GetControllerResultAsync<ActionResult>(c => c.DeleteRestrictedArea(Guid.NewGuid()));
    }
}