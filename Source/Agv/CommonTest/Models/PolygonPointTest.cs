using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Models;

[TestClass]
public class PolygonPointTest
{
    [TestMethod]
    public void OperatorPlus_ZeroVector_ReturnsSameCoordinates()
    {
        var point = new PolygonPoint(3.0, 5.0);

        var result = point + Vector2.Zero;

        result.X.ShouldBe(3.0);
        result.Y.ShouldBe(5.0);
    }

    [TestMethod]
    public void OperatorPlus_PositiveVector_TranslatesCorrectly()
    {
        var point = new PolygonPoint(1.0, 2.0);

        var result = point + new Vector2(3.0f, 4.0f);

        result.X.ShouldBe(4.0, 1e-6);
        result.Y.ShouldBe(6.0, 1e-6);
    }

    [TestMethod]
    public void OperatorPlus_ReturnsNewInstance_OriginalUnchanged()
    {
        var point = new PolygonPoint(1.0, 2.0);

        var result = point + new Vector2(10.0f, 20.0f);

        result.ShouldNotBeSameAs(point);
        point.X.ShouldBe(1.0);
        point.Y.ShouldBe(2.0);
    }
}
