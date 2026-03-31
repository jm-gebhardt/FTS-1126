using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Extensions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Graphs;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Test.Domain.Extensions;

[TestClass]
public class NodePositionExtensionsTest
{
    [TestMethod]
    public void CreateLine_ReturnsDegreeOneNurbs()
    {
        var start = new NodePosition(1.0, 2.0);
        var end = new NodePosition(3.0, 4.0);

        var result = start.CreateLine(end);

        result.Degree.ShouldBe(1);
    }

    [TestMethod]
    public void CreateLine_HasTwoControlPoints()
    {
        var start = new NodePosition(1.0, 2.0);
        var end = new NodePosition(3.0, 4.0);

        var result = start.CreateLine(end);

        result.ControlPoints.Count.ShouldBe(2);
        result.ControlPoints.First().X.ShouldBe(1.0);
        result.ControlPoints.First().Y.ShouldBe(2.0);
        result.ControlPoints.Last().X.ShouldBe(3.0);
        result.ControlPoints.Last().Y.ShouldBe(4.0);
    }

    [TestMethod]
    public void CreateLine_KnotVector()
    {
        var start = new NodePosition(0, 0);
        var end = new NodePosition(1, 1);

        var result = start.CreateLine(end);

        result.KnotVector.ShouldBe([0f, 0f, 1f, 1f]);
    }
}