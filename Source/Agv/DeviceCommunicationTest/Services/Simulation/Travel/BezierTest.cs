using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Travel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation.Travel;

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
    private GraphNode Way2 { get; } = new("Way2") { X = 3, Y = 3 };

    private GraphNode Src3 { get; } = new("Src3") { X = 5, Y = 1 };

    private GraphNode C2 { get; } = new("C2") { X = 3, Y = 1 };

    private GraphEdge Edge { get; } = GraphEdge.CreateBezier2("Way2", "Src3", 4, 5, 3, 3, 1);

    private GraphEdge StraightEdge { get; } = GraphEdge.CreateBezier2("C2", "Src3", 2, 3.5, 1, 4.5, 1);

    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var bezier = new BezierEdge(Edge, Way2, Src3);
        bezier.SourceNode.ShouldBe(Way2);
        bezier.TargetNode.ShouldBe(Src3);
        bezier.ReferenceTarget.X.ShouldBeInRange(Way2.X, Src3.X);
        bezier.ReferenceTarget.Y.ShouldBeInRange(Src3.Y, Way2.Y);
    }

    [TestMethod]
    public void SetAngles_NoFollowUpNode_AnglesSet()
    {
        var bezier = new BezierEdge(Edge, Way2, Src3);
        double entryAngle = Mathematics.ConvertToRadians(90);
        bezier.SetAngles(entryAngle, null, null);
        bezier.EntryAngle.ShouldBe(entryAngle);
        bezier.ExitAngle.ShouldBe(0);
    }

    [TestMethod]
    public void GetCurrentPosition_NoAnglesAtExitTime_PositionIsTargetPosition()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var bezier = new BezierEdge(Edge, Way2, Src3);
        bezier.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        bezier.EstimateTravelTime(agvData, 0, entryTime);
        var position = bezier.GetCurrentPosition(bezier.EstimatedExitTime, agvData, 0);
        position.X.ShouldBe(Src3.X);
        position.Y.ShouldBe(Src3.Y);
        position.Z.ShouldBe(Src3.Z);
    }

    [TestMethod]
    public void GetCurrentPosition_StopForInitialTurn_AtHalfOfTurnTime_OrientationIsHalfOfFinalAndPreviousOrientation()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var bezier = new BezierEdge(StraightEdge, C2, Src3);
        bezier.SetAngles(Mathematics.ConvertToRadians(90), null, null);
        var entryTime = DateTime.UtcNow;
        bezier.EstimateTravelTime(agvData, 0, entryTime);
        const float previousOrientation = 0;
        var finalPosition = bezier.GetCurrentPosition(bezier.EstimatedExitTime, agvData, previousOrientation);

        var position = bezier.GetCurrentPosition(entryTime.AddSeconds((bezier.InitialTurnFinishTime - entryTime).TotalSeconds / 2), agvData, previousOrientation);

        position.Theta.ShouldBe((finalPosition.Theta - previousOrientation) / 2, tolerance: 0.001);
    }

    [TestMethod]
    public void GetCurrentPosition_StopForInitialTurn_AtEndOfTurnTime_OrientationIsFinalOrientation()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var bezier = new BezierEdge(StraightEdge, C2, Src3);
        bezier.SetAngles(Mathematics.ConvertToRadians(90), null, null);
        var entryTime = DateTime.UtcNow;
        bezier.EstimateTravelTime(agvData, 0, entryTime);
        const float previousOrientation = 0;
        var finalPosition = bezier.GetCurrentPosition(bezier.EstimatedExitTime, agvData, previousOrientation);

        var position = bezier.GetCurrentPosition(bezier.InitialTurnFinishTime, agvData, previousOrientation);

        position.Theta.ShouldBe(finalPosition.Theta, tolerance: 0.001);
    }
}