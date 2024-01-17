using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Testing.Time;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

public class SimulatedAgvSagaTestFixture
{
    private const int SimulationSpeed = 1;
    private readonly FakeTimeProvider _timeProvider = new();

    private int NumberOfNodes => NumberOfEdges + 1;

    private readonly DateTime _startTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private ICollection<GraphNode>? _nodes;
    private string _startNodeId = AgvGraphGenerator.SourceClose.Id;
    private string _targetNodeId = AgvGraphGenerator.DestinationClose.Id;
    private GraphNode? _currentNode;
    private AgvGraph? _graph;
    private GraphEdge? _edge;

    private int NumberOfEdges { get; set; } = 1;

    public SimulatedAgvSaga Saga { get; private set; } = null!;

    private AgvAction Action { get; set; } = null!;

    public SimulatedAgvSagaTestFixture WithGraph(AgvGraph graph)
    {
        _graph = graph;
        return this;
    }

    public SimulatedAgvSagaTestFixture WithPosition(GraphNode node)
    {
        _currentNode = node;
        return this;
    }

    public SimulatedAgvSagaTestFixture WithNumberOfEdges(int numberOfEdges)
    {
        NumberOfEdges = numberOfEdges;
        return this;
    }

    public SimulatedAgvSagaTestFixture WithTheseNodes(ICollection<GraphNode> nodes)
    {
        _nodes = nodes;
        return this;
    }

    public SimulatedAgvSagaTestFixture WithActionStartNodeId(string id)
    {
        _startNodeId = id;
        return this;
    }

    public SimulatedAgvSagaTestFixture WithActionTargetNodeId(string id)
    {
        _targetNodeId = id;
        _edge = GraphEdge.CreateDirect(_startNodeId, _targetNodeId, (float)Mathematics.EuclideanDistance(_nodes!.First(n=>n.Id==_startNodeId), _nodes!.First(n => n.Id==_targetNodeId)));
        return this;
    }

    public SimulatedAgvSagaTestFixture Build()
    {
        if (_edge != null)
        {
            _graph ??= AgvGraphGenerator.Generate(_nodes!.ToList(), new List<GraphEdge> { _edge! });
        }
        else
        {
            _graph ??= AgvGraphGenerator.CreateDefaultGraph(nodes: _nodes);
        }

        Action = AgvActionGenerator.GenerateMoveAction(
            startNodeId: _startNodeId,
            targetNodeId: _targetNodeId,
            numberOfNodes: NumberOfNodes,
            availableNodes: (_nodes ?? _graph.Nodes.Where(n => n.Type == AgvGraph.NodeType.WayPoint)).ToList());

        Saga = SimulatedAgvSagaGenerator.Generate(
            agvGraph: _graph,
            currentAction: Action,
            simulationSpeed: SimulationSpeed,
            currentNode: _currentNode,
            timeProvider: _timeProvider
        );
        Saga.ActionStartDate = _startTime;
        var nodes = new List<string>();
        if (_nodes != null)
        {
            nodes.AddRange(_nodes.Select(n => n.Id));
        }
        Saga.TravelSimulator.EstimateTravelTimes(nodes, _startTime, 0);
        return this;
    }

    public void SimulateToPercentage(double progressPercentage)
    {
        double progress = Saga.TravelSimulator.TravelDuration!.Value.TotalSeconds * progressPercentage / 100; 
        _timeProvider.SetNow(_startTime.AddSeconds(progress));
    }
}