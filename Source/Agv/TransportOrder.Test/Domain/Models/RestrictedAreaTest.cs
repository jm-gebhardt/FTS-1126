using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using Gebhardt.Acc.TransportOrder.Domain.Models.RestrictedArea;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.TransportOrder.Test.Domain.Models;

[TestClass]
public class RestrictedAreaTest
{
    [TestMethod]
    public void Construct_FromPolygon_PropertiesSet()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(2, 2, 1) };

        var restrictedArea = new RestrictedArea(polygon);

        restrictedArea.Id.ShouldNotBe(Guid.Empty);
        restrictedArea.Area.Count.ShouldBe(polygon.Count);

        foreach (var point in restrictedArea.Area.Select((location, index) => new { location, index }))
        {
            point.location.LocationId.ShouldBe(restrictedArea.Id);
            point.location.X.ShouldBe(polygon[point.index].X);
            point.location.Y.ShouldBe(polygon[point.index].Y);
        }
    }

    [DataTestMethod]
    [DataRow(2, 2, true)]
    [DataRow(4, 2, false)]
    public void IsPositionInside_ValidData_ReturnsExpectedValue(float x, float y, bool expectedValue)
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

        restrictedArea.IsPositionInside(x, y).ShouldBe(expectedValue);
    }

    [TestMethod]
    public void UpdateArea_AddPoints_AreaUpdated()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);
        List<PolygonPoint> updatedArea = polygon;
        updatedArea.Add(new PolygonPoint(4, 1, 4));
        
        restrictedArea.UpdateArea(updatedArea);

        restrictedArea.Area.Count.ShouldBeEquivalentTo(updatedArea.Count);
    }

    [TestMethod]
    public void UpdateArea_RemovePoints_AreaUpdated()
    {
        var polygon = new List<PolygonPoint> { new(1, 1, 0), new(1, 3, 1), new(3, 3, 2), new(3, 1, 3) };
        var restrictedArea = new RestrictedArea(polygon);

        restrictedArea.UpdateArea([]);

        restrictedArea.Area.ShouldBeEmpty();
    }
}