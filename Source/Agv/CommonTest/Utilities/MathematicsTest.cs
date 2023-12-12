using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.CommonTest.Utilities;

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
        Mathematics.AngleBetween(new Vector2(1, 0), new Vector2( -1, 0)).ShouldBe(Math.PI, 0.0001);
        Mathematics.AngleBetween(new Vector2(1, 0), new Vector2(1, 0)).ShouldBe(0, 0.0001);
        Mathematics.AngleBetween(new Vector2(1, 0), new Vector2(0, 1)).ShouldBe(Math.PI / 2d, 0.0001);
        Mathematics.AngleBetween(new Vector2(-1, 0), new Vector2(1, 0)).ShouldBe(Math.PI, 0.0001);
        Mathematics.AngleBetween(new Vector2(1, 1), new Vector2(1, 0)).ShouldBe(Math.PI / 4d, 0.0001);
    }

    [TestMethod]
    public void EuclideanDistance_ValidInput_CorrectResult()
    {
        Mathematics.EuclideanDistance(new GraphNode("") {X=0, Y=0, Z=0}, new GraphNode("") { X=1, Y=0, Z=0 }).ShouldBe(1, 0.0001);
        Mathematics.EuclideanDistance(new GraphNode("") {X=0, Y=0, Z=0}, new GraphNode("") { X=0, Y=0, Z=0 }).ShouldBe(0, 0.0001);
        Mathematics.EuclideanDistance(new GraphNode("") {X=0, Y=0, Z=0}, new GraphNode("") { X=0, Y=1, Z=0 }).ShouldBe(1, 0.0001);
        Mathematics.EuclideanDistance(new GraphNode("") {X=0, Y=0, Z=0}, new GraphNode("") { X=0, Y=0, Z=1 }).ShouldBe(1, 0.0001);
        Mathematics.EuclideanDistance(new GraphNode("") {X=0, Y=0, Z=1}, new GraphNode("") { X=0, Y=0, Z=-1 }).ShouldBe(2, 0.0001);
        Mathematics.EuclideanDistance(new GraphNode("") {X=0, Y=1, Z=0}, new GraphNode("") { X=1, Y=0, Z=0 }).ShouldBe(1.4142, 0.0001);
    }
}