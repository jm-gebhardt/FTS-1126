using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.Services.Simulation.Travel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation.Travel;

[TestClass]
public class SlantedEdgeTest
{
    ///////// Graph with slanted /////////////////
    // 0 ---- 1 ---- 2 ---- 3 ---- 4 ---- 5 ---- //
    // |                                         //
    // 1                                Src3     //
    // |                                  |      //
    // 2                    ---------------      //
    // |                    |                    //
    // 3                  Way2                   //
    // |                                         //
    ///////////////////////////////////////////////
    private GraphNode Source { get; } = new("Way2") { X = 3, Y = 3 };

    private GraphNode Target { get; } = new("Src3") { X = 5, Y = 1 };

    private GraphEdge Edge { get; } = GraphEdge.CreateSlanted("Way2", "Src3", 4, 0, 1);

    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var slanted = new SlantedEdge(Edge, Source, Target);
        slanted.SourceNode.ShouldBe(Source);
        slanted.TargetNode.ShouldBe(Target);
        slanted.ReferenceTarget.X.ShouldBe(3);
        slanted.ReferenceTarget.Y.ShouldBe(2);
    }

    [TestMethod]
    public void SetAngles_NoFollowUpNode_AnglesSet()
    {
        var slanted = new SlantedEdge(Edge, Source, Target);
        double entryAngle = Mathematics.ConvertToRadians(90);
        slanted.SetAngles(entryAngle, null, null);
        slanted.EntryAngle.ShouldBe(entryAngle);
        slanted.ExitAngle.ShouldBe(0);
    }

    [TestMethod]
    public void SetAngles_FollowUpNode_AnglesSet()
    {
        var slanted = new SlantedEdge(Edge, Source, Target);
        double entryAngle = Mathematics.ConvertToRadians(90);
        slanted.SetAngles(entryAngle, Target, NodeGenerator.GenerateNode(nodeId:"", x: 4, y:0));
        slanted.EntryAngle.ShouldBe(entryAngle);
        slanted.ExitAngle.ShouldBe(Mathematics.ConvertToRadians(45), tolerance: 0.001);
    }

    [TestMethod]
    public void GetCurrentPosition_NoAnglesAtExitTime_PositionIsTargetPosition()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var slanted = new SlantedEdge(Edge, Source, Target);
        slanted.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        slanted.EstimateTravelTime(agvData, 0, entryTime);
        var position = slanted.GetCurrentPosition(slanted.EstimatedExitTime, agvData, 0);
        position.X.ShouldBe(Target.X);
        position.Y.ShouldBe(Target.Y, tolerance: 0.002);
        position.Z.ShouldBe(Target.Z);
    }
}