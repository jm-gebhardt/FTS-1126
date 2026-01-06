using System.Numerics;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Utilities;

[TestClass]
public class MathematicsTest
{
    [TestMethod]
    [DataRow(180, Math.PI)]
    [DataRow(360, 2d * Math.PI)]
    [DataRow(0, 0)]
    [DataRow(5, 0.087266)]
    public void ConvertToRadians_ValidData_CorrectResult(double degrees, double radianResult)
    {
        Mathematics.ConvertToRadians(degrees).ShouldBe(radianResult, 0.0001);
    }

    [TestMethod]
    public void AngleBetween_ValidData_CorrectResult()
    {
        Mathematics.AngleBetween(new Vector2(1, 0), new Vector2(-1, 0)).ShouldBe(Math.PI, 0.0001);
        Mathematics.AngleBetween(new Vector2(1, 0), new Vector2(1, 0)).ShouldBe(0, 0.0001);
        Mathematics.AngleBetween(new Vector2(1, 0), new Vector2(0, 1)).ShouldBe(Math.PI / 2d, 0.0001);
        Mathematics.AngleBetween(new Vector2(-1, 0), new Vector2(1, 0)).ShouldBe(Math.PI, 0.0001);
        Mathematics.AngleBetween(new Vector2(1, 1), new Vector2(1, 0)).ShouldBe(Math.PI / 4d, 0.0001);
    }

    [TestMethod]
    public void AngleBetween_ZeroVector_ReturnsNaN()
    {
        Mathematics.AngleBetween(new Vector2(1, 0), Vector2.Zero).ShouldBe(double.NaN);
        Mathematics.AngleBetween(Vector2.Zero, new Vector2(1, 0)).ShouldBe(double.NaN);
        Mathematics.AngleBetween(Vector2.Zero, Vector2.Zero).ShouldBe(double.NaN);
    }

    [TestMethod]
    public void EuclideanDistance_ValidInput_CorrectResult()
    {
        Mathematics.EuclideanDistance(NodeGenerator.GenerateNode(nodeName: "", x: 0, y: 0, z: 0), NodeGenerator.GenerateNode(nodeName: "", x: 1, y: 0, z: 0)).ShouldBe(1, 0.0001);
        Mathematics.EuclideanDistance(NodeGenerator.GenerateNode(nodeName: "", x: 0, y: 0, z: 0), NodeGenerator.GenerateNode(nodeName: "", x: 0, y: 0, z: 0)).ShouldBe(0, 0.0001);
        Mathematics.EuclideanDistance(NodeGenerator.GenerateNode(nodeName: "", x: 0, y: 0, z: 0), NodeGenerator.GenerateNode(nodeName: "", x: 0, y: 1, z: 0)).ShouldBe(1, 0.0001);
        Mathematics.EuclideanDistance(NodeGenerator.GenerateNode(nodeName: "", x: 0, y: 0, z: 0), NodeGenerator.GenerateNode(nodeName: "", x: 0, y: 0, z: 1)).ShouldBe(1, 0.0001);
        Mathematics.EuclideanDistance(NodeGenerator.GenerateNode(nodeName: "", x: 0, y: 0, z: 1), NodeGenerator.GenerateNode(nodeName: "", x: 0, y: 0, z: -1)).ShouldBe(2, 0.0001);
        Mathematics.EuclideanDistance(NodeGenerator.GenerateNode(nodeName: "", x: 0, y: 1, z: 0), NodeGenerator.GenerateNode(nodeName: "", x: 1, y: 0, z: 0)).ShouldBe(1.4142, 0.0001);
    }

    [TestMethod]
    public void RotateMoveVector3_ValidData_CorrectResult()
    {
        Mathematics.RotateMoveVector3(new Vector3(0, 10, 0), 0, 0).ShouldBe(new Vector3(0, 10, 0));
        Mathematics.RotateMoveVector3(new Vector3(10, 10, 0), 0, 10).ShouldBe(new Vector3(10, 0, 0));
        Mathematics.RotateMoveVector3(new Vector3(10, 10, 0), Math.PI, 10).ShouldBe(new Vector3(10, 20, 0));
        Mathematics.RotateMoveVector3(new Vector3(10, 10, 0), Math.PI * 2d, 10).ShouldBe(new Vector3(10, 0, 0));
        Mathematics.RotateMoveVector3(new Vector3(10, 10, 0), Math.PI * 0.5d, 10).ShouldBe(new Vector3(0, 10, 0));
        Mathematics.RotateMoveVector3(new Vector3(10, 10, 0), Math.PI * -0.5d, 10).ShouldBe(new Vector3(20, 10, 0));
    }

    [TestMethod]
    [DataRow(0, 0, 1, 0, Math.PI / 2)]           // Vector pointing right → 90°
    [DataRow(0, 0, 0, 1, Math.PI)]               // Vector pointing up → 180°
    [DataRow(0, 0, -1, 0, 3 * Math.PI / 2)]      // Vector pointing left → 270° 
    [DataRow(0, 0, 0, -1, 0)]                    // Vector pointing down → 0°
    [DataRow(0, 0, 1, 1, 3 * Math.PI / 4)]       // Vector pointing up-right (45°) → 135°
    [DataRow(0, 0, -1, 1, 5 * Math.PI / 4)]      // Vector pointing up-left (135°) → 225°
    [DataRow(0, 0, 1, -1, Math.PI / 4)]          // Vector pointing down-right (-45°) → 45°
    [DataRow(0, 0, -1, -1, -Math.PI / 4)]        // Vector pointing down-left (-135°) → -45°
    [DataRow(1, 1, 2, 1, Math.PI / 2)]           // Non-origin start, right → 90°
    [DataRow(5, 3, 5, 8, Math.PI)]               // Non-origin start, up → 180°
    public void OrientationToXAxis_ValidVectors_CorrectResult(double startX, double startY, double endX, double endY, double expectedRadians)
    {
        var start = new Vector2((float)startX, (float)startY);
        var end = new Vector2((float)endX, (float)endY);

        Mathematics.OrientationToXAxis(start, end).ShouldBe(expectedRadians, 0.0001);
    }

    [TestMethod]
    public void OrientationToXAxis_IdenticalPoints_ReturnsNaN()
    {
        var point = new Vector2(5, 3);

        Mathematics.OrientationToXAxis(point, point).ShouldBe(double.NaN);
    }

    [TestMethod]
    public void OrientationToXAxis_ZeroVectors_ReturnsNaN()
    {
        Mathematics.OrientationToXAxis(Vector2.Zero, Vector2.Zero).ShouldBe(double.NaN);
    }
}