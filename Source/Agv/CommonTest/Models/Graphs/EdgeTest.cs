using System.Numerics;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Shouldly;

namespace Gebhardt.Acc.CommonTest.Models.Graphs;

[TestClass]
public class EdgeTest
{
    [TestMethod]
    [DataRow(0, 0, 10, 10, 0, 5, 32.3606f)]
    [DataRow(0, 0, 0, 10, 0, 5, 30)]
    [DataRow(0, 0, 10, 0, 0, 5, 24.1421f)]
    [DataRow(0, 0, 0, 10, (float)Math.PI, 5, 10)]
    [DataRow(0, 0, 10, 0, (float)Math.PI, 5, 24.1421f)]
    [DataRow(0, 0, 10, 10, (float)Math.PI, 5, 20)]
    public void SlantedGetLength_ValidData_CorrectResult(float x1, float y1, float x2, float y2, float angle, float distance, float result)
    {
        var slanted = new Slanted(new Vector3(x1, y1, 0f), new Vector3(x2, y2, 0f), angle, distance);
        slanted.GetLength().ShouldBe(result, 0.0001);
    }

    [TestMethod]
    public void MultiEuclideanDistance_ValidData_CorrectResult()
    {
        new Polypath([new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0)]).GetLength().ShouldBe(3, 0.0001);
    }
}