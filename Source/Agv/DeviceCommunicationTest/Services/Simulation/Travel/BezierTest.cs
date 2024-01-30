using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation.Travel;

[TestClass]
public class BezierTest
{
    ///////// Graph with Bezier2  /////////////////
    // 0 ---- 1 ---- 2 ---- 3 ---- 4 ---- 5 ---- //
    // |                                         //
    // 1                   C2             Src3   //
    // |                                         //
    // 2                                         //
    // |                                         //
    // 3                  Way2             C1    //
    // |                                         //
    ///////////////////////////////////////////////
    private GraphNode Source { get; } = new("Way2") { X = 3, Y = 3 };

    private GraphNode Target { get; } = new("Src3") { X = 5, Y = 1 };

    private GraphEdge Edge { get; } = GraphEdge.CreateBezier2("Way2", "Src3", 4, 5, 3, 3, 1);

    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var bezier = new BezierEdge(Edge, Source, Target);
        bezier.SourceNode.ShouldBe(Source);
        bezier.TargetNode.ShouldBe(Target);
        bezier.ReferenceTarget.X.ShouldBeInRange(Source.X, Target.X);
        bezier.ReferenceTarget.Y.ShouldBeInRange(Target.Y, Source.Y);
    }

    [TestMethod]
    public void SetAngles_NoFollowUpNode_AnglesSet()
    {
        var bezier = new BezierEdge(Edge, Source, Target);
        double entryAngle = Mathematics.ConvertToRadians(90);
        bezier.SetAngles(entryAngle, null, null);
        bezier.EntryAngle.ShouldBe(entryAngle);
        bezier.ExitAngle.ShouldBe(0);
    }

    [TestMethod]
    public void GetCurrentPosition_NoAnglesAtExitTime_PositionIsTargetPosition()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var bezier = new BezierEdge(Edge, Source, Target);
        bezier.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        bezier.EstimateTravelTime(agvData, 0, entryTime);
        var position = bezier.GetCurrentPosition(bezier.EstimatedExitTime, agvData, 0);
        position.X.ShouldBe(Target.X);
        position.Y.ShouldBe(Target.Y);
        position.Z.ShouldBe(Target.Z);
    }
}