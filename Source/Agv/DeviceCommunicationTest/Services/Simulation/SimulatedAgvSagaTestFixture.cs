using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Testing.Helpers;
using Gebhardt.StoreWare.Agv.Common.Testing.Time;
using Gebhardt.StoreWare.Agv.DeviceCommunication.States;
using Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Helper;

namespace Gebhardt.StoreWare.Agv.DeviceCommunicationTest.Services.Simulation;

public class SimulatedAgvSagaTestFixture
{
    private const int NodeTravelTime = 10;
    private readonly FakeTimeProvider _timeProvider = new();

    private int NumberOfNodes => NumberOfEdges + 1;

    private readonly DateTime _startTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private ICollection<GraphNode>? _nodes;
    private string _startNodeId = AgvGraphGenerator.SourceClose.Id;
    private string _targetNodeId = AgvGraphGenerator.DestinationClose.Id;
    private GraphNode? _currentNode;

    public int NumberOfEdges { get; private set; } = 1;

    public SimulatedAgvSaga Saga { get; private set; } = null!;

    public AgvAction Action { get; private set; } = null!;

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
        return this;
    }

    public SimulatedAgvSagaTestFixture Build()
    {
        var graph = AgvGraphGenerator.CreateDefaultGraph(nodes: _nodes);

        Action = AgvActionGenerator.GenerateMoveAction(
            startNodeId: _startNodeId,
            targetNodeId: _targetNodeId,
            numberOfNodes: NumberOfNodes,
            availableNodes: (_nodes ?? graph.Nodes.Where(n => n.Type == AgvGraph.NodeType.WayPoint)).ToList());

        Saga = SimulatedAgvSagaGenerator.Generate(
            agvGraph: graph,
            currentAction: Action,
            nodeTravelTime: NodeTravelTime,
            currentNode: _currentNode,
            timeProvider: _timeProvider
        );
        Saga.ActionStartDate = _startTime;
        return this;
    }

    public void SimulateToPercentage(decimal progressPercentage)
    {
        double ms = (double) progressPercentage * NumberOfEdges / NumberOfNodes * NodeTravelTime * NumberOfEdges / 100;
        _timeProvider.SetNow(_startTime.AddTicks((long) (ms * 10000)));
    }
}