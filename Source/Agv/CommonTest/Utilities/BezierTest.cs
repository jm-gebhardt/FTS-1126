using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Utilities;

[TestClass]
public class BezierTest
{
    [TestMethod]
    public void DeCasteljau_ValidData_CorrectResult()
    {
        var bezier = new Bezier(new List<Vector2> { new(0, 0), new(0, 1), new(1, 1), new(1, 0) });
        var middle = bezier.DeCasteljau(0.5);
        middle.X.ShouldBe(0.5f, 0.0001);
        middle.Y.ShouldBe(0.75f, 0.0001);
    }

    [TestMethod]
    public void DeCasteljau_ValidData_CorrectTangent()
    {
        var bezier = new Bezier(new List<Vector2> { new(0, 0), new(0, 1), new(1, 1), new(1, 0) });
        _ = bezier.DeCasteljau(0.5, out var tangentPoint1, out var tangentPoint2);
        tangentPoint1.X.ShouldBe(0.25f, 0.0001);
        tangentPoint1.Y.ShouldBe(0.75f, 0.0001);
        tangentPoint2.X.ShouldBe(0.75f, 0.0001);
        tangentPoint2.Y.ShouldBe(0.75f, 0.0001);
    }

    [TestMethod]
    public void DeCasteljau_EdgeCase_ReturnsControlPolygon()
    {
        var bezier = new Bezier(new List<Vector2> { new(0, 0), new(0, 1), new(1, 1), new(1, 0) });
        var controlPoint1 = bezier.DeCasteljau(0);
        var controlPoint2 = bezier.DeCasteljau(1);

        controlPoint1.X.ShouldBe(0f, 0.0001);
        controlPoint1.Y.ShouldBe(0f, 0.0001);

        controlPoint2.X.ShouldBe(1f, 0.0001);
        controlPoint2.Y.ShouldBe(0f, 0.0001);
    }

    [TestMethod]
    public void InterpolateCurve_ValidData_ReturnsInterpolation()
    {
        var bezier = new Bezier(new List<Vector2> { new(0, 0), new(0, 1), new(1, 1), new(1, 0) });

        ICollection<Vector2> interpolation = bezier.InterpolateCurve(100);
        interpolation.Count.ShouldBe(100);

        // Check symmetry
        for (int i = 0; i <= 50; ++i)
        {
            var point1 = interpolation.ElementAt(i);
            var point2 = interpolation.ElementAt(99 - i);

            point1.X.ShouldBe(1 - point2.X, 0.00001);
            point1.Y.ShouldBe(point2.Y, 0.00001);
        }
    }

    [TestMethod]
    public void GetLength_ValidData_CorrectResult()
    {
        var bezier = new Bezier(new List<Vector2> { new(0, 0), new(0, 1), new(1, 1), new(1, 0) });
        double length = bezier.GetLength(1000);
        length.ShouldBe(2, 0.00001);

        bezier = new Bezier(new List<Vector2> { new(0, 0), new(0, 1), new(0, 2), new(0, 3) });
        length = bezier.GetLength(1000);
        length.ShouldBe(3, 0.00001);
    }

    [TestMethod]
    public void BezierConstructor_InvalidData_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new Bezier(new List<Vector2>()));
        Should.Throw<ArgumentOutOfRangeException>(() => new Bezier(new List<Vector2> { new(0, 0) }));
        Should.Throw<ArgumentOutOfRangeException>(() => new Bezier(new List<Vector2> { new(0, 0), new(0, 1) }));
    }

    [TestMethod]
    public void InterpolateCurve_InvalidData_ThrowsArgumentOutOfRangeException()
    {
        var bezier = new Bezier(new List<Vector2> { new(0, 0), new(0, 1), new(0, 2), new(0, 3) });
        Should.Throw<ArgumentOutOfRangeException>(() => bezier.InterpolateCurve(1));
        Should.Throw<ArgumentOutOfRangeException>(() => bezier.InterpolateCurve(0));
        Should.Throw<ArgumentOutOfRangeException>(() => bezier.InterpolateCurve(-1));
    }

    [TestMethod]
    public void DeCasteljau_InvalidData_ThrowsArgumentOutOfRangeException()
    {
        var bezier = new Bezier(new List<Vector2> { new(0, 0), new(0, 1), new(0, 2), new(0, 3) });
        Should.Throw<ArgumentOutOfRangeException>(() => bezier.DeCasteljau(-1));
        Should.Throw<ArgumentOutOfRangeException>(() => bezier.DeCasteljau(2));
    }

    [TestMethod]
    public void GetLength_InvalidData_ThrowsArgumentOutOfRangeException()
    {
        var bezier = new Bezier(new List<Vector2> { new(0, 0), new(0, 1), new(0, 2), new(0, 3) });
        Should.Throw<ArgumentOutOfRangeException>(() => bezier.GetLength(1));
        Should.Throw<ArgumentOutOfRangeException>(() => bezier.GetLength(0));
        Should.Throw<ArgumentOutOfRangeException>(() => bezier.GetLength(-1));
    }
}