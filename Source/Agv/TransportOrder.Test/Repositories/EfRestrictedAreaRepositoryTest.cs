using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

    [DataTestMethod]
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

        var polygon = new List<PolygonPoint> { new(1, 1), new(1, 3), new(3, 3), new(3, 1) };
        var restrictedArea = new RestrictedArea(polygon);
        await using var fixture = new EfRestrictedAreaRepositoryTestFixture();
        await fixture.AddAsync(restrictedArea);

        (await fixture.Repository.IsPointInRestrictedArea(x,y)).ShouldBe(expectedValue);
    }
}