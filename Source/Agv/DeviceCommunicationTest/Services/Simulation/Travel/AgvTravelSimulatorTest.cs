using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Testing.Helpers;
using Shouldly;
using Gebhardt.Acc.Common.Testing.Time;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Data;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Maps;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Travel;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation.Travel;

[TestClass]
public class AgvTravelSimulatorTest
{
    private ( string[] nodes, Guid[]edges) GetArbitraryRoute(string targetNode, AgvGraph graph)
    {
        var edges = new List<Guid>();
        var nodes = new List<string> { targetNode };
        foreach (int unused in Enumerable.Range(0, 3))
        {
            var edge = graph.Edges.FirstOrDefault(e => e.ToNodeId == targetNode && !nodes.Contains(e.FromNodeId));
            if (edge == null)
            {
                break;
            }
            nodes.Add(edge.FromNodeId);
            edges.Add(edge.Id);
            targetNode = edge.FromNodeId;
        }
        nodes.Reverse();
        edges.Reverse();

        return (nodes.ToArray(), edges.ToArray());
    }

    private readonly AgvGraph _wikiExample = new(Guid.Empty,
        new[]
        {
            NodeGenerator.GenerateNode(nodeId: "Nairobi", nodeType: AgvGraph.NodeType.PickDrop, x: 0f, y: 0f, z: 0f),
            NodeGenerator.GenerateNode(nodeId: "Tokyo", nodeType: AgvGraph.NodeType.PickDrop, x: 42f, y: 80f, z: 0f),
            NodeGenerator.GenerateNode(nodeId: "Marseille", nodeType: AgvGraph.NodeType.PickDrop, x: 104f, y: 100f, z: 0f),
            NodeGenerator.GenerateNode(nodeId: "Berlin", nodeType: AgvGraph.NodeType.PickDrop, x: 96f, y: 60f, z: 0f),
            NodeGenerator.GenerateNode(nodeId: "Denver", nodeType: AgvGraph.NodeType.PickDrop, x: 104f, y: -10f, z: 0f),
            NodeGenerator.GenerateNode(nodeId: "Helsinki", nodeType: AgvGraph.NodeType.PickDrop, x: 150f, y: 20f, z: 0f),
            NodeGenerator.GenerateNode(nodeId: "Rio", nodeType: AgvGraph.NodeType.PickDrop, x: 200f, y: 92f, z: 0f)
        },
        new[]
        {
            GraphEdge.CreateDirect("Nairobi", "Tokyo", 70f),
            GraphEdge.CreateDirect("Nairobi", "Denver", 145f),
            GraphEdge.CreateDirect("Denver", "Nairobi", 145f),
            GraphEdge.CreateDirect("Tokyo", "Marseille", 103f),
            GraphEdge.CreateDirect("Tokyo", "Berlin", 53f),
            GraphEdge.CreateDirect("Denver", "Helsinki", 84f),
            GraphEdge.CreateDirect("Marseille", "Rio", 116f),
            GraphEdge.CreateDirect("Berlin", "Rio", 183f),
            GraphEdge.CreateDirect("Helsinki", "Rio", 102f)
        });

    private readonly AgvData _agvData = AgvDataGenerator.GenerateAgvData(acceleration: 1, maxVelocityStraight: 1, maxVelocityTurn: 0.9, maxVelocityTurnOnSpot: Math.PI, turnOnSpotThreshold: 0.08726646);

    [TestMethod]
    public void Construct_ValidData_EdgesAndPositionSet()
    {
        var correlationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var position = new AgvPosition(true, 1, 1, 1, 0);
        var simulator = new AgvTravelSimulator(agvGraph, position, new FakeTimeProvider());
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
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(agvData.CorrelationId);
        (string[] nodes, Guid[] edges) route = GetArbitraryRoute("PD-QA", graph);
        var startNode = graph.Nodes.First(n => n.Id == route.nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(graph, position, new FakeTimeProvider());

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(route.nodes, route.edges, entryTime, agvData, initialVelocity);
        simulator.FollowingEdges.Count.ShouldBe(route.nodes.Length - 1);
        simulator.TravelDuration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        simulator.FollowingEdges.ForEach(e =>
        {
            e.EntryTime.ShouldBeGreaterThanOrEqualTo(entryTime);
            e.EstimatedExitTime.ShouldBeGreaterThanOrEqualTo(entryTime);
        });
        simulator.CurrentEdge!.SourceNode.ShouldBe(startNode);
        simulator.FollowingEdges.Last().TargetNode.Id.ShouldBe(route.nodes.Last());
    }

    [TestMethod]
    public void EstimateTravelTimes_FullFixedRoute_EdgesSet()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = { "D-Paint", "WP-0016", "WP-0015", "WP-0014", "WP-0029", "Park-1" };
        Guid[] edges = AgvGraphGenerator.SelectRouteEdges(nodes.ToList(), graph.Edges.ToList());
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(graph, position, new FakeTimeProvider());

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(nodes, edges, entryTime, agvData, initialVelocity);
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
    public void EstimateTravelTimes_MoveToCoordinate_PropertiesSet()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = { "D-Paint", "WP-0016" };
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(graph, position, new FakeTimeProvider());

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        var coordinates = graph.Nodes.Single(n => n.Id == nodes.Last());
        simulator.EstimateTravelTimes(new AgvPosition(true, coordinates.X, coordinates.Y, coordinates.Z, coordinates.Theta), entryTime, agvData, initialVelocity);
        simulator.TravelDuration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        simulator.FollowingEdges.Count.ShouldBe(1);
    }

    [TestMethod]
    public void EstimateTravelTimes_MoveFromCoordinateBackToGraph_PropertiesSet()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = { "D-Paint", "WP-0016" };
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var fakeTimeProvider = new FakeTimeProvider();
        var simulator = new AgvTravelSimulator(graph, position, fakeTimeProvider);

        // Move to coordinate
        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        var coordinates = graph.Nodes.Single(n => n.Id == nodes.Last());
        simulator.EstimateTravelTimes(new AgvPosition(true, coordinates.X, coordinates.Y, coordinates.Z, coordinates.Theta), entryTime, agvData, initialVelocity);
        fakeTimeProvider.SetNow(entryTime.Add(simulator.TravelDuration!.Value));
        simulator.UpdatePosition(agvData);
        simulator.FinishTravel();

        // Move to node on graph
        string target = startNode.Id;
        simulator.EstimateTravelTimes([target], [], entryTime, agvData, initialVelocity);
        simulator.FollowingEdges.Single().TargetNode.Id.ShouldBe(target);
        simulator.TravelDuration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    [TestMethod]
    public void EstimateTravelTimes_MoveFromCoordinateToCoordinate_PropertiesSet()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = { "D-Paint", "WP-0016" };
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var fakeTimeProvider = new FakeTimeProvider();
        var simulator = new AgvTravelSimulator(graph, position, fakeTimeProvider);

        // Move to coordinate
        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        var coordinates = graph.Nodes.Single(n => n.Id == nodes.Last());
        simulator.EstimateTravelTimes(new AgvPosition(true, coordinates.X, coordinates.Y, coordinates.Z, coordinates.Theta), entryTime, agvData, initialVelocity);
        fakeTimeProvider.SetNow(entryTime.Add(simulator.TravelDuration!.Value));
        simulator.UpdatePosition(agvData);
        simulator.FinishTravel();

        // Move to next coordinate
        var nextCoordinates = graph.Nodes.Single(n => n.Id == nodes.First());
        simulator.EstimateTravelTimes(new AgvPosition(true, nextCoordinates.X, nextCoordinates.Y, nextCoordinates.Z, nextCoordinates.Theta), entryTime, agvData, initialVelocity);
        fakeTimeProvider.SetNow(entryTime.Add(simulator.TravelDuration!.Value));
        simulator.UpdatePosition(agvData);
        simulator.TravelDuration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        simulator.FollowingEdges.Count.ShouldBe(1);
    }

    [TestMethod]
    public void EstimateTravelTimes_WikipediaExample() // For comparison with identical test of a*
    {
        string[] nodes = { "Nairobi", "Tokyo", "Marseille", "Rio" };
        var startNode = _wikiExample.Nodes.First(n => n.Id == nodes.First());
        Guid[] edges = AgvGraphGenerator.SelectRouteEdges(nodes.ToList(), _wikiExample.Edges.ToList());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(_wikiExample, position, new FakeTimeProvider());

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(nodes, edges, entryTime, _agvData, initialVelocity);
        simulator.FollowingEdges.Count.ShouldBe(nodes.Length - 1);
        simulator.TravelDuration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        double turnForwardCost = Mathematics.ConvertToRadians(180) / _agvData.MaxVelocityTurnOnSpot;
        simulator.TravelDuration!.Value.TotalSeconds.ShouldBe(291.8725786 + turnForwardCost, 0.51);
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
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = { "D-Press", "WP-0011", "WP-0016", "WP-0015", "WP-0017", "WP-0123", "PD-QA" };
        Guid[] edges = AgvGraphGenerator.SelectRouteEdges(nodes.ToList(), graph.Edges.ToList());
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(graph, position, new FakeTimeProvider());

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(nodes, edges, entryTime, agvData, initialVelocity);
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
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var agvGraph = generator.Generate(agvData.CorrelationId);

        var position = new AgvPosition(true, 1, 1, 1, 0);
        var simulator = new AgvTravelSimulator(agvGraph, position, new FakeTimeProvider());

        simulator.EstimateTravelTimes(new List<string>(), [], DateTime.UtcNow, agvData, 0);

        simulator.CurrentEdge.ShouldBeNull();
        simulator.FollowingEdges.ShouldBeEmpty();
        simulator.TravelDuration.ShouldBeNull();
    }

    [TestMethod]
    public void EstimateTravelTimes_SingleNodeChargeAction_PositionSetToNode()
    {
        var correlationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var agvData = new KarisType1(correlationId, "ip address");
        var node = agvGraph.Nodes.First(n => n.IsCharger());
        var position = new AgvPosition(true, node.X, node.Y, node.Z, node.Theta);
        var fakeTimeProvider = new FakeTimeProvider(DateTime.UtcNow);
        var simulator = new AgvTravelSimulator(agvGraph, position, fakeTimeProvider);

        //Mock previous move action to charger:
        simulator.GetType().GetProperty(nameof(AgvTravelSimulator.CurrentNode))!.SetValue(simulator, node);

        simulator.EstimateTravelTimes(new List<string> { node.Id }, [], fakeTimeProvider.Now(), agvData, 0);

        simulator.CurrentEdge.ShouldBeNull();
        simulator.CurrentNode.ShouldBe(node);
        simulator.FollowingEdges.ShouldBeEmpty();
        simulator.TravelDuration.ShouldBe(TimeSpan.Zero);

        simulator.UpdatePosition(agvData);
        simulator.Position.ShouldBe(position);
    }

    [TestMethod]
    public void EstimateTravelTimes_OneEdge_IsDriven()
    {
        var correlationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var agvData = new KarisType1(correlationId, "ip address");
        var edge = agvGraph.Edges.First();
        var sourceNode = agvGraph.Nodes.Single(n => n.Id == edge.FromNodeId);
        var targetNode = agvGraph.Nodes.Single(n => n.Id == edge.ToNodeId);
        var position = new AgvPosition(true, sourceNode.X, sourceNode.Y, sourceNode.Z, sourceNode.Theta);
        var fakeTimeProvider = new FakeTimeProvider(DateTime.UtcNow);
        var simulator = new AgvTravelSimulator(agvGraph, position, fakeTimeProvider);
        var time = DateTime.UtcNow;

        simulator.EstimateTravelTimes(new List<string> { sourceNode.Id, targetNode.Id }, [edge.Id], time, agvData, 0);

        simulator.CurrentEdge.ShouldBe(edge);
        simulator.CurrentNode.ShouldBe(sourceNode);
        simulator.FollowingEdges.Count.ShouldBe(1);
        simulator.FollowingEdges.Single().SourceNode.ShouldBe(sourceNode);
        simulator.FollowingEdges.Single().TargetNode.ShouldBe(targetNode);
        simulator.TravelDuration.ShouldNotBeNull();

        fakeTimeProvider.SetNow(time + simulator.TravelDuration!.Value);
        simulator.UpdatePosition(agvData);
        simulator.CurrentNode.ShouldBe(targetNode);
        simulator.Position.ShouldBe(new AgvPosition(true, targetNode.X, targetNode.Y, targetNode.Z, simulator.FollowingEdges.Single().AgvOrientation));
    }

    [TestMethod]
    public void EstimateTravelTimes_FromStationToEdgeWithZeroEntryAngle_AgvStopsAndTurnsBetweenEdges()
    {
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var agvGraph = generator.Generate(agvData.CorrelationId);
        string[] nodes = { "D-Paint", "WP-0016", "WP-0015" };
        Guid[] edges = AgvGraphGenerator.SelectRouteEdges(nodes.ToList(), agvGraph.Edges.ToList());
        var startNode = agvGraph.Nodes.First(n => n.Id == nodes.First());
        float orientationOnEdgeAfterStation = (float) Mathematics.ConvertToRadians(90);
        float orientationOnNextEdge = (float) Mathematics.ConvertToRadians(270);
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, orientationOnEdgeAfterStation);
        var simulator = new AgvTravelSimulator(agvGraph, position, new FakeTimeProvider());

        simulator.EstimateTravelTimes(nodes, edges, DateTime.UtcNow, agvData, 0);
        var edgeAfterStation = simulator.FollowingEdges.First();
        var nextEdge = simulator.FollowingEdges.Last();

        // agv stops
        edgeAfterStation.ExitVelocity.ShouldBe(0);
        nextEdge.InitialVelocity.ShouldBe(0);
        nextEdge.InitialTurnFinishTime.ShouldNotBe(nextEdge.EntryTime);

        // and turns
        edgeAfterStation.AgvOrientation.ShouldBe(orientationOnEdgeAfterStation);
        nextEdge.AgvOrientation.ShouldBe(orientationOnNextEdge);
    }

    [TestMethod]
    public void UpdatePosition_ValidData_PositionAndCurrentEdgeUpdated()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(agvData.CorrelationId);
        (string[] nodes, Guid[] edges) route = GetArbitraryRoute("PD-QA", graph);
        var startNode = graph.Nodes.First(n => n.Id == route.nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var fakeTimeProvider = new FakeTimeProvider(DateTime.UtcNow);
        var simulator = new AgvTravelSimulator(graph, position, fakeTimeProvider);

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(route.nodes, route.edges, entryTime, agvData, initialVelocity);

        fakeTimeProvider.SetNow(entryTime.Add(simulator.TravelDuration!.Value));
        simulator.UpdatePosition(agvData);

        simulator.Position.ShouldNotBe(position);
        simulator.CurrentEdge.ShouldBe(simulator.FollowingEdges.Last());
        simulator.CurrentNode.ShouldBe(graph.Nodes.First(n => n.Id == route.nodes.Last()));
    }

    [TestMethod]
    public void UpdatePosition_NoFollowingEdges_PositionAndCurrentEdgeNotUpdated()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var position = new AgvPosition(true, 1, 1, 1, 0);
        var simulator = new AgvTravelSimulator(agvGraph, position, new FakeTimeProvider());

        simulator.UpdatePosition(agvData);
        simulator.Position.ShouldBe(position);
        simulator.CurrentEdge.ShouldBeNull();
    }

    [TestMethod]
    public void UpdatePosition_TargetStationReached_AgvStaysAtOrientationOfEdge()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(agvData.CorrelationId);
        (string[] nodes, Guid[] edges) route = GetArbitraryRoute("PD-QA", graph);
        var startNode = graph.Nodes.First(n => n.Id == route.nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var entryTime = DateTime.UtcNow;
        var fakeTimeProvider = new FakeTimeProvider(entryTime);
        var simulator = new AgvTravelSimulator(graph, position, fakeTimeProvider);

        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(route.nodes, route.edges, entryTime, agvData, initialVelocity);

        var targetNode = graph.Nodes.First(n => n.Id == route.nodes.Last());
        fakeTimeProvider.SetNow(entryTime.Add(0.95 * simulator.TravelDuration!.Value));
        simulator.UpdatePosition(agvData);
        simulator.CurrentNode.ShouldNotBe(targetNode);

        float lastOrientationOnEdge = simulator.Position.Theta;

        fakeTimeProvider.SetNow(entryTime.Add(simulator.TravelDuration!.Value));
        simulator.UpdatePosition(agvData);
        simulator.Position.Theta.ShouldBe(lastOrientationOnEdge);
        simulator.CurrentEdge.ShouldBe(simulator.FollowingEdges.Last());
        simulator.CurrentNode.ShouldBe(targetNode);
    }

    [TestMethod]
    public void UpdatePosition_MoveToCoordinate_TargetReached_OrientationOfTarget()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = { "D-Paint", "WP-0016" };
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var entryTime = DateTime.UtcNow;
        var fakeTimeProvider = new FakeTimeProvider(entryTime);
        var simulator = new AgvTravelSimulator(graph, position, fakeTimeProvider);

        const double initialVelocity = 0;
        var targetCoordinate = graph.Nodes.Single(n => n.Id == nodes.Last());
        simulator.EstimateTravelTimes(new AgvPosition(true, targetCoordinate.X, targetCoordinate.Y, targetCoordinate.Z, targetCoordinate.Theta), entryTime, agvData, initialVelocity);

        fakeTimeProvider.SetNow(entryTime.Add(simulator.TravelDuration!.Value));
        simulator.UpdatePosition(agvData);
        simulator.Position.Theta.ShouldBe(targetCoordinate.Theta);
        simulator.CurrentEdge.ShouldBe(simulator.FollowingEdges.Last());
        simulator.CurrentNode!.X.ShouldBe(targetCoordinate.X);
        simulator.CurrentNode!.Y.ShouldBe(targetCoordinate.Y);
    }

    [TestMethod]
    public void UpdatePosition_MoveFromCoordinateBackToGraph_TargetReached_OrientationOfStation()
    {
        var correlationId = Guid.NewGuid();
        var agvData = new KarisType1(correlationId, "ip address");
        var generator = new FixedGraphGenerator(new GeneratedMapGenerator());
        var graph = generator.Generate(agvData.CorrelationId);
        string[] nodes = { "D-Paint", "WP-0016" };
        var startNode = graph.Nodes.First(n => n.Id == nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var fakeTimeProvider = new FakeTimeProvider();
        var simulator = new AgvTravelSimulator(graph, position, fakeTimeProvider);

        // Move to coordinate
        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        var coordinates = graph.Nodes.Single(n => n.Id == nodes.Last());
        simulator.EstimateTravelTimes(new AgvPosition(true, coordinates.X, coordinates.Y, coordinates.Z, coordinates.Theta), entryTime, agvData, initialVelocity);
        fakeTimeProvider.SetNow(entryTime.Add(simulator.TravelDuration!.Value));
        simulator.UpdatePosition(agvData);
        simulator.FinishTravel();

        // Move to node on graph
        var target = startNode;
        simulator.EstimateTravelTimes([target.Id], [], entryTime, agvData, initialVelocity);
        fakeTimeProvider.SetNow(entryTime.Add(simulator.TravelDuration!.Value));
        simulator.UpdatePosition(agvData);
        simulator.Position.Theta.ShouldBe(target.Theta);
    }

    [TestMethod]
    public void FinishTravel_ValidData_FollowingEdgesTravelDurationReset()
    {
        var correlationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var agvData = new KarisType1(correlationId, "ip address");
        var action = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: correlationId).Actions.First();
        (string[] nodes, Guid[] edges) route = GetArbitraryRoute(action.Target.NodeId!, agvGraph);
        action.Nodes = route.nodes;
        action.Edges = route.edges;
        var startNode = agvGraph.Nodes.First(n => n.Id == action.Nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var simulator = new AgvTravelSimulator(agvGraph, position, new FakeTimeProvider());

        var entryTime = DateTime.UtcNow;
        const double initialVelocity = 0;
        simulator.EstimateTravelTimes(action.Nodes.ToArray(), route.edges, entryTime, agvData, initialVelocity);

        simulator.FollowingEdges.Count.ShouldBe(action.Nodes.Count - 1);
        simulator.TravelDuration!.Value.ShouldBeGreaterThan(TimeSpan.Zero);
        simulator.CurrentEdge!.SourceNode.ShouldBe(startNode);

        simulator.FinishTravel();

        simulator.CurrentEdge.ShouldBeNull();
        simulator.FollowingEdges.ShouldBeEmpty();
        simulator.TravelDuration.ShouldBeNull();
        simulator.CurrentNode.ShouldNotBeNull();
    }

    [TestMethod]
    public void PauseTime_ConsidersPreviousAndActivePause()
    {
        var correlationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var action = AgvActionGenerator.GeneratePickDropActionGroup(assignedAgv: correlationId).Actions.First();
        (string[] nodes, Guid[] edges) route = GetArbitraryRoute(action.Target.NodeId!, agvGraph);
        action.Nodes = route.nodes;
        action.Edges = route.edges;
        var startNode = agvGraph.Nodes.First(n => n.Id == action.Nodes.First());
        var position = new AgvPosition(true, startNode.X, startNode.Y, startNode.Z, startNode.Theta);
        var fakeTimeProvider = new FakeTimeProvider();
        var simulator = new AgvTravelSimulator(agvGraph, position, fakeTimeProvider);
        var pauseFrom = new DateTime(2000, 1, 1, 0, 0, 0);
        fakeTimeProvider.SetNow(pauseFrom);

        simulator.Pause();
        var pauseTo = new DateTime(2000, 1, 1, 0, 0, 1);
        fakeTimeProvider.SetNow(pauseTo);
        simulator.Resume();
        var pauseFrom2 = new DateTime(2000, 1, 1, 0, 0, 10);
        fakeTimeProvider.SetNow(pauseFrom2);
        simulator.Pause();
        var pauseTo2 = new DateTime(2000, 1, 1, 0, 0, 11);
        fakeTimeProvider.SetNow(pauseTo2);

        var pauseTime1 = pauseTo - pauseFrom;
        var activePause = pauseTo2 - pauseFrom2;
        simulator.PauseTime.ShouldBe(pauseTime1 + activePause);
    }

    [TestMethod]
    public void Pause_FreezesPosition()
    {
        var correlationId = Guid.NewGuid();
        var agvGraph = AgvGraphGenerator.CreateDefaultGraph(correlationId);
        var agvData = new KarisType1(correlationId, "ip address");
        var edge = agvGraph.Edges.First();
        var sourceNode = agvGraph.Nodes.Single(n => n.Id == edge.FromNodeId);
        var targetNode = agvGraph.Nodes.Single(n => n.Id == edge.ToNodeId);
        var position = new AgvPosition(true, sourceNode.X, sourceNode.Y, sourceNode.Z, sourceNode.Theta);
        var fakeTimeProvider = new FakeTimeProvider();
        var simulator = new AgvTravelSimulator(agvGraph, position, fakeTimeProvider);
        var time = new DateTime(2000, 1, 1, 0, 0, 0);
        fakeTimeProvider.SetNow(time);
        var nodes = new List<string> { sourceNode.Id, targetNode.Id };
        Guid[] edges = AgvGraphGenerator.SelectRouteEdges(nodes, agvGraph.Edges.ToList());

        simulator.EstimateTravelTimes(nodes, edges, time, agvData, 0);

        simulator.CurrentEdge.ShouldBe(edge);
        simulator.CurrentNode.ShouldBe(sourceNode);
        simulator.FollowingEdges.Count.ShouldBe(1);
        simulator.FollowingEdges.Single().SourceNode.ShouldBe(sourceNode);
        simulator.FollowingEdges.Single().TargetNode.ShouldBe(targetNode);
        simulator.TravelDuration.ShouldNotBeNull();

        var halfNodeTime = simulator.TravelDuration!.Value / 2;
        var currentTime = time + halfNodeTime;
        fakeTimeProvider.SetNow(currentTime);
        simulator.UpdatePosition(agvData);
        simulator.Pause();
        position = simulator.Position;
        fakeTimeProvider.SetNow(currentTime += TimeSpan.FromSeconds(43));
        simulator.UpdatePosition(agvData);
        simulator.Position.ShouldBe(position);
        simulator.Resume();
        fakeTimeProvider.SetNow(currentTime + halfNodeTime);
        simulator.UpdatePosition(agvData);
        simulator.Position.ShouldBe(new AgvPosition(true, targetNode.X, targetNode.Y, targetNode.Z, simulator.FollowingEdges.Single().AgvOrientation));
    }
}