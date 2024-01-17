using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Data;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation.Travel;

[TestClass]
public class AgvTravelSimulatorTest
{
    private string[] GetArbitraryRoute(string targetNode, AgvGraph graph)
    {
        var nodes = new List<string> { targetNode };
        foreach (int unused in Enumerable.Range(0, 3))
        {
            var edge = graph.Edges.FirstOrDefault(e => e.ToNodeId == targetNode && !nodes.Contains(e.FromNodeId));
            if (edge == null)
            {
                break;
            }
            nodes.Add(edge.FromNodeId);
            targetNode = edge.FromNodeId;
        }
        nodes.Reverse();
        return nodes.ToArray();
    }

    [TestMethod]
    public void Construct_ValidData_EdgesAndPositionSet()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var position = new AgvPosition(true, 1, 1, 1, 0);
        var simulator = new AgvTravelSimulator(agvGraph, agvData, position);
        simulator.ShouldNotBeNull();
        simulator.CurrentEdge.ShouldBeNull();
        simulator.Position.ShouldBe(position);
        simulator.TravelDuration.ShouldBeNull();
        simulator.Edges.Count.ShouldBe(agvGraph.Edges.Count);
        simulator.FollowingEdges.ShouldBeEmpty();
        simulator.CurrentNode.ShouldBeNull();
    }

    [TestMethod]
    public void EstimateTravelTimes_ArbitraryRoute_EdgesSet()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator();
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = GetArbitraryRoute("PD-QA", graph);
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(graph, agvData, position);

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(nodes, entryTime, initialVelocity);
        simulator.FollowingEdges.Count.ShouldBe(nodes.Length - 1);
        simulator.TravelDuration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        simulator.FollowingEdges.ForEach(e =>
        {
            e.EntryTime.ShouldBeGreaterThanOrEqualTo(entryTime);
            e.EstimatedExitTime.ShouldBeGreaterThanOrEqualTo(entryTime);
        });
        simulator.CurrentEdge!.SourceNode.ShouldBe(startNode);
        simulator.FollowingEdges.Last().TargetNode.Id.ShouldBe(nodes.Last());
    }

    [TestMethod]
    public void EstimateTravelTimes_FullFixedRoute_EdgesSet()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator();
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = { "D-Paint", "WP-0016", "WP-0015", "WP-0014", "WP-0029", "Park-1" };
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(graph, agvData, position);

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(nodes, entryTime, initialVelocity);
        simulator.FollowingEdges.Count.ShouldBe(nodes.Length - 1);
        simulator.TravelDuration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        simulator.FollowingEdges.ForEach(e =>
        {
            e.EntryTime.ShouldBeGreaterThanOrEqualTo(entryTime);
            e.EstimatedExitTime.ShouldBeGreaterThanOrEqualTo(entryTime);
        });
        simulator.CurrentEdge!.SourceNode.ShouldBe(startNode);
        simulator.FollowingEdges.Last().TargetNode.Id.ShouldBe(nodes.Last());
    }

    [TestMethod]
    public void EstimateTravelTimes_KarisType1_EdgesSet()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator();
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = { "D-Press", "WP-0011", "WP-0016", "WP-0015", "WP-0017", "WP-0123", "PD-QA" };
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(graph, agvData, position);

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(nodes, entryTime, initialVelocity);
        simulator.FollowingEdges.Count.ShouldBe(nodes.Length - 1);
        simulator.TravelDuration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        simulator.FollowingEdges.ForEach(e =>
        {
            e.EntryTime.ShouldBeGreaterThanOrEqualTo(entryTime);
            e.EstimatedExitTime.ShouldBeGreaterThanOrEqualTo(entryTime);
        });
        simulator.CurrentEdge!.SourceNode.ShouldBe(startNode);
        simulator.FollowingEdges.Last().TargetNode.Id.ShouldBe(nodes.Last());
    }

    [TestMethod]
    public void EstimateTravelTimes_NoNodes_FollowingEdgesAndTimesNotSet()
    {
        var generator = new FixedGraphGenerator();
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var agvGraph = generator.Generate(agvData.CorrelationId);

        var position = new AgvPosition(true, 1, 1, 1, 0);
        var simulator = new AgvTravelSimulator(agvGraph, agvData, position);

        simulator.EstimateTravelTimes(new List<string>(), DateTime.UtcNow, 0);

        simulator.CurrentEdge.ShouldBeNull();
        simulator.FollowingEdges.ShouldBeEmpty();
        simulator.TravelDuration.ShouldBeNull();
    }

    [TestMethod]
    public void EstimateTravelTimes_SingleNodeChargeAction_FollowingEdgesAndTimesSet()
    {
        var correlationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var agvData = new KarisType1(correlationId, "ip address");
        var position = new AgvPosition(true, 1, 1, 1, 0);
        var simulator = new AgvTravelSimulator(agvGraph, agvData, position);

        simulator.EstimateTravelTimes(new List<string>{agvGraph.Nodes.First(n=>n.Type == AgvGraph.NodeType.Charge).Id}, DateTime.UtcNow, 0);

        simulator.CurrentEdge.ShouldNotBeNull();
        simulator.FollowingEdges.ShouldNotBeEmpty();
        simulator.TravelDuration.ShouldNotBeNull();
    }

    [TestMethod]
    public void UpdatePosition_ValidData_PositionAndCurrentEdgeUpdated()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator();
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = GetArbitraryRoute("PD-QA", graph);
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(graph, agvData, position);

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(nodes, entryTime, initialVelocity);

        var now = entryTime.Add(simulator.TravelDuration!.Value);
        simulator.UpdatePosition(now);

        simulator.Position.ShouldNotBe(position);
        simulator.CurrentEdge.ShouldBe(simulator.FollowingEdges.Last());
        simulator.CurrentNode.ShouldBe(graph.Nodes.First(n =>n.Id == nodes.Last()));
    }

    [TestMethod]
    public void UpdatePosition_NoFollowingEdges_PositionAndCurrentEdgeNotUpdated()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var position = new AgvPosition(true, 1, 1, 1, 0);
        var simulator = new AgvTravelSimulator(agvGraph, agvData, position);

        simulator.UpdatePosition(DateTime.UtcNow);
        simulator.Position.ShouldBe(position);
        simulator.CurrentEdge.ShouldBeNull();
    }

    [TestMethod]
    public void UpdatePosition_TargetStationReached_AgvFacesBackwards()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator();
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = GetArbitraryRoute("PD-QA", graph);
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(graph, agvData, position);

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(nodes, entryTime, initialVelocity);

        var targetNode = graph.Nodes.First(n => n.Id == nodes.Last());
        var shortlyBeforeTargetReached = entryTime.Add(0.95*simulator.TravelDuration!.Value);
        simulator.UpdatePosition(shortlyBeforeTargetReached);
        simulator.CurrentNode.ShouldNotBe(targetNode);

        float lastOrientationOnEdge = simulator.Position.Theta;

        var targetReached = entryTime.Add(simulator.TravelDuration!.Value);
        simulator.UpdatePosition(targetReached);
        simulator.Position.Theta.ShouldBe(lastOrientationOnEdge - (float)Math.PI);
        simulator.CurrentEdge.ShouldBe(simulator.FollowingEdges.Last());
        simulator.CurrentNode.ShouldBe(targetNode);
    }

    [TestMethod]
    public void FinishTravel_ValidData_FollowingEdgesTravelDurationReset()
    {
        var correlationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var agvData = new KarisType1(correlationId, "ip address");
        var action = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: correlationId).Actions.First();
        action.Nodes = GetArbitraryRoute(action.TargetNodeId, agvGraph);
        var startNode = agvGraph.Nodes.First(n => n.Id == action.Nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(agvGraph, agvData, position);

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(action.Nodes, entryTime, initialVelocity);

        simulator.FollowingEdges.Count.ShouldBe(action.Nodes.Length - 1);
        simulator.TravelDuration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        simulator.CurrentEdge!.SourceNode.ShouldBe(startNode);

        simulator.FinishTravel();

        simulator.CurrentEdge.ShouldBeNull();
        simulator.FollowingEdges.ShouldBeEmpty();
        simulator.TravelDuration.ShouldBeNull();
        simulator.CurrentNode.ShouldNotBeNull();
    }
}