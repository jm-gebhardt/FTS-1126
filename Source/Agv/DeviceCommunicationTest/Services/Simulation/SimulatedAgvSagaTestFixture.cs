using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Testing.Helpers;
using Gebhardt.Acc.Common.Testing.Time;
using Gebhardt.Acc.DeviceCommunication.SimAgv.States;
using Gebhardt.Acc.DeviceCommunicationTest.Helper;

namespace Gebhardt.Acc.DeviceCommunicationTest.Services.Simulation;

public class SimulatedAgvSagaTestFixture
{
    private const int SimulationSpeed = 1;
    private readonly FakeTimeProvider _timeProvider = new();

    private int NumberOfNodes => NumberOfEdges + 1;

    private readonly DateTime _startTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private ICollection<GraphNode>? _nodes;
    private string _startNodeId = AgvGraphGenerator.SourceClose.Name;
    private string _targetNodeId = AgvGraphGenerator.DestinationClose.Name;
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
        _edge = GraphEdge.CreateDirect(_startNodeId.ToGuid(), _targetNodeId.ToGuid(), (float) Mathematics.EuclideanDistance(_nodes!.First(n => n.Name == _startNodeId), _nodes!.First(n => n.Name == _targetNodeId)));
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
            availableNodes: (_nodes ?? _graph.Nodes.Where(n => (AgvGraph.NodeType) n.Type == AgvGraph.NodeType.WayPoint)).ToList());

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
            nodes.AddRange(_nodes.Select(n => n.Name));
        }
        Guid[] edges = AgvGraphGenerator.SelectRouteEdges(nodes, _graph.Edges.ToList());
        Saga.TravelSimulator.EstimateTravelTimes(nodes, edges, _startTime, Saga.AgvData!, 0);
        return this;
    }

    public void SimulateToPercentage(double progressPercentage)
    {
        double progress = Saga.TravelSimulator.TravelDuration!.Value.TotalSeconds * progressPercentage / 100;
        _timeProvider.SetNow(_startTime.AddSeconds(progress));
    }
}