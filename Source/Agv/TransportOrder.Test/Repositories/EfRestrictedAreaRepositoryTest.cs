using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Repositories;

[TestClass]
public class EfRestrictedAreaRepositoryTest
{
    [TestMethod]
    public async Task IsPointInRestrictedArea_NoRestrictedAreas_ReturnsFalse()
    {
        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();

        (await fixture.Repository.IsPointInRestrictedArea(1,1)).ShouldBeFalse();
    }

    [TestMethod]
    [DataRow(2,2,true)]
    [DataRow(4,2,false)]
    public async Task IsPointInRestrictedArea_ValidData_ReturnsExpectedValue(float x, float y, bool expectedValue)
    {
        /*
        Sketched restricted area:

            1 2 3 4
            3 *---*
            2 | x | x
            1 *---*

        Key:
        - "*" represents a polygon corner.
        - "x" represents the location of the point
        */

        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);
        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();
        await fixture.AddAsync(restrictedArea);

        (await fixture.Repository.IsPointInRestrictedArea(x,y)).ShouldBe(expectedValue);
    }

    [TestMethod]
    public async Task GetRestrictedArea_ValidData_GetsRestrictedArea()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);

        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();
        await fixture.AddAsync(restrictedArea);

        (await fixture.Repository.GetRestrictedArea(restrictedArea.Id)).ShouldBeEquivalentTo(restrictedArea);
    }

    [TestMethod]
    public async Task GetRestrictedArea_UnknownArea_ThrowsRestrictedAreaNotFoundException()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);

        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();
        await fixture.AddAsync(restrictedArea);

        await Should.ThrowAsync<RestrictedAreaNotFoundException>(() => fixture.Repository.GetRestrictedArea("unknown".ToGuid()));
    }

    [TestMethod]
    public async Task AddRestrictedArea_ValidData_RestrictedAreaAdded()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);
        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();

        await fixture.Repository.AddRestrictedArea(restrictedArea);

        (await fixture.Repository.GetRestrictedArea(restrictedArea.Id)).ShouldBeEquivalentTo(restrictedArea);
    }

    [TestMethod]
    public async Task GetRestrictedAreas_ValidData_GetsRestrictedAreas()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);

        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();
        await fixture.AddAsync(restrictedArea);

        (await fixture.Repository.GetRestrictedAreas([restrictedArea.Id])).ShouldBeEquivalentTo(new List<RestrictedArea>{restrictedArea});
    }

    [TestMethod]
    public async Task GetRestrictedAreas_NoIdsGiven_GetsAllRestrictedAreas()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);

        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();
        await fixture.AddAsync(restrictedArea);

        (await fixture.Repository.GetRestrictedAreas([])).ShouldBeEquivalentTo(new List<RestrictedArea> { restrictedArea });
    }

    [TestMethod]
    public async Task GetRestrictedAreas_UnknownArea_ThrowsRestrictedAreaNotFoundException()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);

        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();
        await fixture.AddAsync(restrictedArea);

        await Should.ThrowAsync<RestrictedAreaNotFoundException>(() => fixture.Repository.GetRestrictedAreas([restrictedArea.Id, "unknown".ToGuid()]));
    }

    [TestMethod]
    public async Task UpdateRestrictedArea_PointAdded_RestrictedAreaUpdated()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);
        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();
        await fixture.AddAsync(restrictedArea);

        var updatedArea = polygon;
        updatedArea.Add(new PolygonPoint(4, 1, 4));
        restrictedArea.UpdateArea(updatedArea);

        await fixture.Repository.UpdateRestrictedArea(restrictedArea);

        (await fixture.Repository.GetRestrictedArea(restrictedArea.Id)).Area.Count.ShouldBeEquivalentTo(updatedArea.Count);
    }

    [TestMethod]
    public async Task UpdateRestrictedArea_PointsRemoved_RestrictedAreaUpdated()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);
        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();
        await fixture.AddAsync(restrictedArea);

        restrictedArea.UpdateArea([]);

        await fixture.Repository.UpdateRestrictedArea(restrictedArea);

        (await fixture.Repository.GetRestrictedArea(restrictedArea.Id)).Area.ShouldBeEmpty();
    }

    [TestMethod]
    public async Task UpdateRestrictedArea_UnknownArea_ThrowsRestrictedAreaNotFoundException()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);
        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();

        await Should.ThrowAsync<RestrictedAreaNotFoundException>(() => fixture.Repository.UpdateRestrictedArea(restrictedArea));
    }

    [TestMethod]
    public async Task DeleteRestrictedArea_ValidData_RestrictedAreaRemoved()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);
        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();
        await fixture.AddAsync(restrictedArea);

        await fixture.Repository.DeleteRestrictedArea(restrictedArea.Id);

        (await fixture.Repository.GetRestrictedAreas([])).ShouldBeEmpty();
    }
}