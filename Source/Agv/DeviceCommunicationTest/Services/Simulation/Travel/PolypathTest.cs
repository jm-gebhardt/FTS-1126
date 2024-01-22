using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation.Travel;

[TestClass]
public class PolypathTest
{
    ///////// Graph with polypath /////////////////
    // 0 ---- 1 ---- 2 ---- 3 ---- 4 ---- 5 ---- //
    // |                                         //
    // 1                                Src3     //
    // |                                  |      //
    // 2                    C1-----------c2      //
    // |                    |                    //
    // 3                  Way2                   //
    // |                                         //
    ///////////////////////////////////////////////
    private GraphNode Source { get; } = new("Way2") {X = 3, Y = 3};

    private GraphNode Target { get; } = new("Src3") {X = 5, Y = 1};

    private GraphEdge Edge { get; } = GraphEdge.CreatePolypath("Way2", "Src3", 4, new List<EdgeCoefficient> {new(3, 2), new(5, 2)});

    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var polypath = new Polypath(Edge, Source, Target);
        polypath.SourceNode.ShouldBe(Source);
        polypath.TargetNode.ShouldBe(Target);
        polypath.ReferenceTarget.X.ShouldBe((float) Edge.VariantParameters.First().X!);
        polypath.ReferenceTarget.Y.ShouldBe((float) Edge.VariantParameters.First().Y!);
    }

    [TestMethod]
    public void SetAngles_NoFollowUpNode_AnglesSet()
    {
        var polypath = new Polypath(Edge, Source, Target);
        double entryAngle = Mathematics.ConvertToRadians(90);
        polypath.SetAngles(entryAngle, null, null);
        polypath.EntryAngle.ShouldBe(entryAngle);
        polypath.ExitAngle.ShouldBe(0);
    }

    [TestMethod]
    public void SetAngles_FollowUpNode_AnglesSet()
    {
        var polypath = new Polypath(Edge, Source, Target);
        double entryAngle = Mathematics.ConvertToRadians(90);
        polypath.SetAngles(entryAngle, Target, new GraphNode("") {X = 4, Y = 0});
        polypath.EntryAngle.ShouldBe(entryAngle);
        polypath.ExitAngle.ShouldBe(Mathematics.ConvertToRadians(45), tolerance: 0.001);
    }

    [TestMethod]
    public void GetCurrentPosition_NoAnglesAtExitTime_PositionIsTargetPosition()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var polypath = new Polypath(Edge, Source, Target);
        polypath.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        polypath.EstimateTravelTime(agvData, 0, entryTime);
        var position = polypath.GetCurrentPosition(polypath.EstimatedExitTime, agvData, 0);
        position.X.ShouldBe(Target.X);
        position.Y.ShouldBe(Target.Y, tolerance: 0.002);
        position.Z.ShouldBe(Target.Z);
    }

    [TestMethod]
    public void EstimateTravelTime_NonZeroInitialVelocity_InitialVelocitiesSet()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var polypath = new Polypath(Edge, Source, Target);
        polypath.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        const int initialVelocity = 12;
        polypath.EstimateTravelTime(agvData, initialVelocity, entryTime);
        polypath.InitialVelocity.ShouldBe(initialVelocity);
        polypath.Sections.First().InitialVelocity.ShouldBe(initialVelocity);
        foreach (var item in polypath.Sections.Skip(1).Select((section, index) => new {section, index}))
        {
            item.section.InitialVelocity.ShouldBe(polypath.Sections[item.index].ExitVelocity);
        }
    }
}