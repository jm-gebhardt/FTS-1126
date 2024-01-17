using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Data;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation.Travel;

[TestClass]
public class DirectEdgeTest
{
    ///////// Default graph for unit tests /////////
    // 0 ---- 1 ---- 2 ---- 3 ---- 4 ---- 5 ---- //
    // |                                         //
    // 1                   Src2          Src3    //
    // |                                   |     //
    // 2            Src1                   |     //
    // |             |                     |     //
    // 3     Prk1---Way1---Way2----------Prk2    //
    // |             |      |                    //
    // 4     Crg1---Way3---Way4----------Crg2    //
    // |             |      |                    //
    // 5            Dst1    |                    //
    // |                    |                    //
    // 6                   Dst2          Dst3    //
    // |                                         //
    ///////////////////////////////////////////////
    private GraphNode Way2 { get; } = new("Way2") { X = 3, Y = 3 };

    private GraphNode Target { get; } = new("Prk2") { X = 5, Y = 3 };

    private GraphEdge Edge { get; } = GraphEdge.CreateDirect("Way2", "Prk2", 2);

    private AgvTravelSimulator WoodShopSimulator { get; set; } = null!;

    private AgvGraph WoodShop { get; set; } = null!;

    private void SetupWoodShop()
    {
        var generator = new FixedGraphGenerator();
        WoodShop = generator.Generate(Guid.NewGuid());
    }

    private void SetupWoodShopSimulator(AgvData agv, AgvPosition? position = null)
    {
        if (position == null)
        {
            SetupWoodShop();
            var startNode = WoodShop.Nodes.First();
            position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        }
        WoodShopSimulator = new AgvTravelSimulator(WoodShop, agv, position);
    }

    [TestMethod]
    public void Construct_ValidData_PropertiesSet()
    {
        var directEdge = new DirectEdge(Edge, Way2, Target);

        directEdge.SourceNode.ShouldBe(Way2);
        directEdge.TargetNode.ShouldBe(Target);
        directEdge.ReferenceTarget.ShouldBe(Target);
    }

    [TestMethod]
    public void GetCurrentPosition_NoAnglesAtExitTime_PositionIsTargetPosition()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var directEdge = new DirectEdge(Edge, Way2, Target);
        directEdge.SetAngles(0, Target, new GraphNode("new"){X= Target.X+1, Y=Target.Y, Z= Target.Z});
        var entryTime = DateTime.UtcNow;
        directEdge.EstimateTravelTime(agvData, 0, entryTime);
        var position = directEdge.GetCurrentPosition(directEdge.EstimatedExitTime, agvData,0);
        position.X.ShouldBe(Target.X, tolerance:0.01);
        position.Y.ShouldBe(Target.Y, tolerance: 0.01);
        position.Z.ShouldBe(Target.Z, tolerance: 0.01);
    }

    [TestMethod]
    public void GetCurrentPosition_StationAtEdgeExit_PositionIsTargetPosition()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var directEdge = new DirectEdge(Edge, Way2, Target);
        directEdge.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        directEdge.EstimateTravelTime(agvData, 0, entryTime);
        var position = directEdge.GetCurrentPosition(directEdge.EstimatedExitTime, agvData,0);
        position.X.ShouldBe(Target.X, tolerance: 0.01);
        position.Y.ShouldBe(Target.Y, tolerance: 0.01);
        position.Z.ShouldBe(Target.Z, tolerance: 0.01);
    }

    [TestMethod]
    public void GetCurrentPosition_InitialTurning_OnlyThetaIsUpdated()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var directEdge = new DirectEdge(Edge, Way2, Target);
        double entryAngle = Mathematics.ConvertToRadians(90);
        directEdge.SetAngles(entryAngle, null, null);
        var entryTime = DateTime.UtcNow;
        directEdge.EstimateTravelTime(agvData, 0, entryTime);
        // at entry
        var position = directEdge.GetCurrentPosition(entryTime, agvData,0);
        position.Theta.ShouldBe(BaseEdge.GetAgvOrientation(Way2, Target)-(float)entryAngle);
        position.X.ShouldBe(Way2.X, tolerance: 0.01);
        position.Y.ShouldBe(Way2.Y, tolerance: 0.01);
        position.Z.ShouldBe(Way2.Z, tolerance: 0.01);
        // in the middle of turning
        double secondsPassed = (directEdge.InitialTurnFinishTime - directEdge.EntryTime).TotalSeconds / 2d;
        position = directEdge.GetCurrentPosition(directEdge.EntryTime.AddSeconds(secondsPassed), agvData, position.Theta);
        position.Theta.ShouldBe(BaseEdge.GetAgvOrientation(Way2, Target)-(float)entryAngle/2, tolerance:0.001);
        position.X.ShouldBe(Way2.X, tolerance: 0.01);
        position.Y.ShouldBe(Way2.Y, tolerance: 0.01);
        position.Z.ShouldBe(Way2.Z, tolerance: 0.01);
        // at the end of turning
        position = directEdge.GetCurrentPosition(directEdge.InitialTurnFinishTime, agvData, position.Theta);
        position.Theta.ShouldBe(BaseEdge.GetAgvOrientation(Way2, Target));
        position.X.ShouldBe(Way2.X, tolerance: 0.01);
        position.Y.ShouldBe(Way2.Y, tolerance: 0.01);
        position.Z.ShouldBe(Way2.Z, tolerance: 0.01);
    }

    [TestMethod]
    public void SetAngles_PrkWay1Src3_ExitAndEntryAngleSet()
    {
        var nextSource = new GraphNode("ns") { X = 5, Y = 3 };
        var nextTarget = new GraphNode("nt") { X = 5, Y = 1 };

        var directEdge = new DirectEdge(Edge, Way2, Target);
        directEdge.ExitAngle.ShouldBe(0);
        directEdge.EntryAngle.ShouldBe(0);

        const double entryAngle = 12;
        directEdge.SetAngles(entryAngle, nextSource, nextTarget);
        directEdge.EntryAngle.ShouldBe(entryAngle);
        directEdge.ExitAngle.ShouldBe(Mathematics.ConvertToRadians(90));
    }

    [TestMethod]
    public void EstimateTravelTime_NoAdjacentEdgesTargetIsStation_SectionsAndVelocitiesSet()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var target = new GraphNode(Target.Id) { X = Target.X, Y = Target.Y, Z = Target.Z, Type = AgvGraph.NodeType.Charge };
        var directEdge = new DirectEdge(Edge, Way2, target);
        directEdge.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        directEdge.EstimateTravelTime(agvData, 0, entryTime);
        directEdge.ExitVelocity.ShouldBe(0);
        directEdge.Length.ShouldBe(directEdge.MaxVelocitySection+directEdge.AccelerationSection+directEdge.DecelerationSection, tolerance:0.01);
    }

    [TestMethod]
    public void EstimateTravelTime_NoAnglesAtExitNextTargetIsNotStation_SectionsAndVelocitiesSet()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var directEdge = new DirectEdge(Edge, Way2, Target);
        var entryTime = DateTime.UtcNow;
        directEdge.SetAngles(0, Target, new GraphNode("new") { X= Target.X+1, Y=Target.Y, Z= Target.Z });
        directEdge.EstimateTravelTime(agvData, 0, entryTime);
        directEdge.Length.ShouldBe(directEdge.MaxVelocitySection+directEdge.AccelerationSection+directEdge.DecelerationSection, tolerance: 0.01);
        directEdge.ExitVelocity.ShouldBe(agvData.MaxVelocityStraight);
        directEdge.MaxPossibleVelocity.ShouldBe(agvData.MaxVelocityStraight);
        directEdge.Length.ShouldBe(directEdge.MaxVelocitySection+directEdge.AccelerationSection+directEdge.DecelerationSection, tolerance: 0.01);
    }

    [TestMethod]
    public void EstimateTravelTime_EdgeToShortForAcceleratingToMaxVelocityAndExitVelocity_AccelerateToMaxPossibleVelocityOnLength()
    {
        const double initialVelocity = 0;
        var directEdge = new DirectEdge(Edge, Way2, Target);
        directEdge.SetAngles(0, Target, new GraphNode("new") { X= Target.X+1, Y=Target.Y, Z= Target.Z });
        var karis = new KarisType1(Guid.NewGuid(), "ip address");
        var agvData = karis.SetSimulationSpeed(5);
        double distance= PhysicsHelper.DistanceToVelocityFromVelocity(agvData.Acceleration, agvData.MaxVelocityStraight, initialVelocity);
        distance.ShouldBeGreaterThan(directEdge.Length);
        double expectedExitVelocity = PhysicsHelper.VelocityAfterDistanceFromVelocity(agvData.Acceleration, directEdge.Length, initialVelocity);
        var entryTime = DateTime.UtcNow;
        directEdge.EstimateTravelTime(agvData, initialVelocity, entryTime);
        
        directEdge.Length.ShouldBe(directEdge.MaxVelocitySection+directEdge.AccelerationSection+directEdge.DecelerationSection, tolerance: 0.01);
        directEdge.DecelerationSection.ShouldBe(0);
        directEdge.MaxVelocitySection.ShouldBe(0);
        directEdge.ExitVelocity.ShouldBeLessThan(agvData.MaxVelocityStraight);
        directEdge.ExitVelocity.ShouldBe(expectedExitVelocity);
    }

    [TestMethod]
    public void GetEdgeTravelDistance_InitialTurnTime_DistanceIsZero()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var directEdge = new DirectEdge(Edge, Way2, Target);
        directEdge.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        directEdge.EstimateTravelTime(agvData, 0, entryTime);

        double distance = directEdge.GetEdgeTravelDistance(agvData, directEdge.InitialTurnFinishTime);
        distance.ShouldBe(0);
    }

    [TestMethod]
    public void GetEdgeTravelDistance_EndOfAcceleration_DistanceIsAccelerationSection()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var directEdge = new DirectEdge(Edge, Way2, Target);
        directEdge.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        directEdge.EstimateTravelTime(agvData, 0, entryTime);

        double distance = directEdge.GetEdgeTravelDistance(agvData, directEdge.AccelerationFinishTime);
        distance.ShouldBe(directEdge.AccelerationSection);
    }

    [TestMethod]
    public void GetEdgeTravelDistance_EndOfMaxVelocity_DistanceIsAccelerationSectionPlusMaxVelocitySection()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var directEdge = new DirectEdge(Edge, Way2, Target);
        directEdge.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        directEdge.EstimateTravelTime(agvData, 0, entryTime);

        double distance = directEdge.GetEdgeTravelDistance(agvData, directEdge.MaxVelocityFinishTime);
        distance.ShouldBe(directEdge.AccelerationSection+directEdge.MaxVelocitySection);
    }

    [TestMethod]
    public void GetEdgeTravelDistance_AtExit_DistanceIsLength()
    {
        var agvData = AgvDataGenerator.GenerateAgvData();
        var directEdge = new DirectEdge(Edge, Way2, Target);
        directEdge.SetAngles(0, null, null);
        var entryTime = DateTime.UtcNow;
        directEdge.EstimateTravelTime(agvData, 0, entryTime);

        double distance = directEdge.GetEdgeTravelDistance(agvData, directEdge.EstimatedExitTime);
        distance.ShouldBe(directEdge.Length, tolerance: 0.01);
    }

    [TestMethod]
    public void GetEdgeTravelDistance_Karis1WoodShopAtEnd_DistanceIsLength()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        SetupWoodShop();
        string[] nodes = { "D-Press", "WP-0011", "WP-0016", "WP-0015", "WP-0017", "WP-0123", "PD-QA" };
        var startNode = WoodShop.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        SetupWoodShopSimulator(agvData, position);
        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        WoodShopSimulator.EstimateTravelTimes(nodes, entryTime, initialVelocity);

        var directEdge = (DirectEdge)WoodShopSimulator.FollowingEdges.Single(e => e.SourceNode.Id == "WP-0017");
        double distanceAcc = directEdge.GetEdgeTravelDistance(agvData, directEdge.AccelerationFinishTime);
        distanceAcc.ShouldBe(directEdge.AccelerationSection, tolerance: 0.01);

        double distance = directEdge.GetEdgeTravelDistance(agvData, directEdge.EstimatedExitTime);
        distance.ShouldBe(directEdge.Length, tolerance:0.01);
        var time = directEdge.MaxVelocityFinishTime.AddSeconds((directEdge.EstimatedExitTime - directEdge.MaxVelocityFinishTime).TotalSeconds * 0.8);
        double almostDistance = directEdge.GetEdgeTravelDistance(agvData,time);
        almostDistance.ShouldBeLessThan(distance);
    }

    [TestMethod]
    public void GetPartialOrientation_WoodShopWay11Way16ToWay16Way15_LeftTurn()
    {
        var agvData = new KarisType1(Guid.NewGuid(), "ip address");
        SetupWoodShopSimulator(agvData);

        var edge = WoodShopSimulator.Edges.Single(e => e.SourceNode.Id == "WP-0016" && e.TargetNode.Id == "WP-0015");
        var previousEdge = WoodShopSimulator.Edges.Single(e => e.SourceNode.Id == "WP-0011" && e.TargetNode.Id == "WP-0016");

        edge.AgvOrientation.ShouldBe((float)Mathematics.ConvertToRadians(270));
        previousEdge.AgvOrientation.ShouldBe((float)Mathematics.ConvertToRadians(0));

        var entryTime = DateTime.UtcNow;
        var initialTurnTime = entryTime.AddSeconds(10);
        var now = initialTurnTime;

        // when turn finished - agv is oriented to the edge
        float orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe(edge.AgvOrientation);

        //at entry - agv is oriented to the previous edge
        now = entryTime;
        orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe(previousEdge.AgvOrientation);

        // at half time of turn - agv is oriented by half to the edge (315 degrees)
        now = entryTime.AddSeconds((initialTurnTime-entryTime).TotalSeconds/2d);
        orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe((float)Mathematics.ConvertToRadians(315), tolerance:0.001);
    }

    [TestMethod]
    public void GetPartialOrientation_WoodShopWayDPressWay11ToWay11Way16_RightTurn()
    {
        var agvData = new KarisType1(Guid.NewGuid(), "ip address");
        SetupWoodShopSimulator(agvData);

        var edge = WoodShopSimulator.Edges.Single(e => e.SourceNode.Id == "WP-0011" && e.TargetNode.Id == "WP-0016");
        var previousEdge = WoodShopSimulator.Edges.Single(e => e.SourceNode.Id == "D-Press" && e.TargetNode.Id == "WP-0011");

        edge.AgvOrientation.ShouldBe((float)Mathematics.ConvertToRadians(0));
        previousEdge.AgvOrientation.ShouldBe((float)Mathematics.ConvertToRadians(270));

        var entryTime = DateTime.UtcNow;
        var initialTurnTime = entryTime.AddSeconds(10);
        var now = initialTurnTime;

        // when turn finished - agv is oriented to the edge
        float orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe(edge.AgvOrientation);

        //at entry - agv is oriented to the previous edge
        now = entryTime;
        orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe(previousEdge.AgvOrientation);

        // at half time of turn - agv is oriented by half to the edge (315 degrees)
        now = entryTime.AddSeconds((initialTurnTime-entryTime).TotalSeconds/2d);
        orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe((float)Mathematics.ConvertToRadians(315), tolerance: 0.001);
    }

    [TestMethod]
    public void GetPartialOrientation_WoodShopPark4Way28ToWay28Way29_LeftTurn()
    {
        var agvData = new KarisType1(Guid.NewGuid(), "ip address");
        SetupWoodShopSimulator(agvData);

        var edge = WoodShopSimulator.Edges.Single(e => e.SourceNode.Id == "WP-0028" && e.TargetNode.Id == "WP-0029");
        var previousEdge = WoodShopSimulator.Edges.Single(e => e.SourceNode.Id == "Park-4" && e.TargetNode.Id == "WP-0028");

        edge.AgvOrientation.ShouldBe((float)Mathematics.ConvertToRadians(180));
        previousEdge.AgvOrientation.ShouldBe((float)Mathematics.ConvertToRadians(270));

        var entryTime = DateTime.UtcNow;
        var initialTurnTime = entryTime.AddSeconds(10);
        var now = initialTurnTime;

        // when turn finished - agv is oriented to the edge
        float orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe(edge.AgvOrientation);

        //at entry - agv is oriented to the previous edge
        now = entryTime;
        orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe(previousEdge.AgvOrientation);

        // at half time of turn - agv is oriented by half to the edge (225 degrees)
        now = entryTime.AddSeconds((initialTurnTime-entryTime).TotalSeconds/2d);
        orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe((float)Mathematics.ConvertToRadians(225), tolerance: 0.001);
    }

    [TestMethod]
    public void GetPartialOrientation_WoodShopPark3Way28ToWay28Way29_LeftTurn()
    {
        var agvData = new KarisType1(Guid.NewGuid(), "ip address");
        SetupWoodShopSimulator(agvData);

        var edge = WoodShopSimulator.Edges.Single(e => e.SourceNode.Id == "WP-0028" && e.TargetNode.Id == "WP-0029");
        var previousEdge = WoodShopSimulator.Edges.Single(e => e.SourceNode.Id == "Park-3" && e.TargetNode.Id == "WP-0028");

        edge.AgvOrientation.ShouldBe((float)Mathematics.ConvertToRadians(180));
        previousEdge.AgvOrientation.ShouldBe((float)Mathematics.ConvertToRadians(90));

        var entryTime = DateTime.UtcNow;
        var initialTurnTime = entryTime.AddSeconds(10);
        var now = initialTurnTime;

        // when turn finished - agv is oriented to the edge
        float orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe(edge.AgvOrientation);

        //at entry - agv is oriented to the previous edge
        now = entryTime;
        orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe(previousEdge.AgvOrientation);

        // at half time of turn - agv is oriented by half to the edge (135 degrees)
        now = entryTime.AddSeconds((initialTurnTime-entryTime).TotalSeconds/2d);
        orientation = BaseEdge.GetPartialOrientation(now, initialTurnTime, entryTime, edge.AgvOrientation, previousEdge.AgvOrientation);
        orientation.ShouldBe((float)Mathematics.ConvertToRadians(135), tolerance: 0.001);
    }
}
