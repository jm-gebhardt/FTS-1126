using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Time;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.States;

/// <summary>
/// Agv Saga for simulated AGVs
/// </summary>
[PublicAPI]
public class SimulatedAgvSaga : SagaStateMachineInstance
{
    private readonly ITimeProvider _timeProvider;
    private const int AliveChargeThreshold = 5;
    private const float InitialTheta = 0f;

    private readonly AgvGraph? _agvGraph;

    /// <summary>
    /// Value for simulating change in battery charge
    /// </summary>
    public double SimulatedBatteryCharge { get; set; }
    
    /// <summary>
    /// Value for simulating change in battery charge [%/s]
    /// </summary>
    public double BatteryChargeRate { get; set; }

    /// <summary>
    /// Current node of this AGV
    /// </summary>
    public GraphNode? CurrentNode { get; set; }

    /// <summary>
    /// Correlation ID
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <inheritdoc cref="Agv" />
    public string AgvId { get; }

    /// <inheritdoc cref="Agv" />
    public string CurrentState { get; set; }

    /// <inheritdoc cref="Agv" />
    public bool IsOnline { get; set; } = true;

    /// <inheritdoc cref="Agv" />
    public bool IsLocked { get; set; }

    /// <inheritdoc cref="Agv" />
    public bool HasError { get; }

    /// <inheritdoc cref="Agv" />
    public bool GraphExists { get; set; }

    /// <inheritdoc cref="Agv" />
    public DateTime TimeOfStateTransition { get; set; } = DateTime.UtcNow;

    /// <inheritdoc cref="Agv" />
    public DateTime TimeOfLockStateChanged { get; set; } = DateTime.UtcNow;

    /// <inheritdoc cref="Agv" />
    public AgvStatus AgvStatus { get; private set; }
    
    /// <summary>
    /// We still save the graph in the simulated agv for convenience.
    /// </summary>
    public AgvGraph? AgvGraph
    {
        get => _agvGraph;
        init
        {
            _agvGraph = value;

            if (AgvGraph == null || Position != null)
            {
                return;
            }

            if (CurrentNode == null)
            {
                SetRandomPosition();
            }
            else
            {
                SetPosition(CurrentNode);
            }
        }
    }

    private void SetRandomPosition()
    {
        ICollection<GraphNode>? nodes = AgvGraph?.Nodes;
        if (nodes == null)
        {
            return;
        }

        List<GraphNode> graphNodes = nodes.ToList();
        List<GraphNode> stations = graphNodes.Where(n => n.Type switch
        {
            AgvGraph.NodeType.Pick => true,
            AgvGraph.NodeType.Drop => true,
            AgvGraph.NodeType.PickDrop => true,
            _ => false
        }).ToList();

        // we have no stations, so we just pick a random node
        var node = !stations.Any() ? graphNodes.ElementAt(Random.Shared.Next(0, graphNodes.Count)) : stations[Random.Shared.Next(0, stations.Count)];
        SetPosition(node);
    }

    private void SetPosition(GraphNode node)
    {
        Position = new AgvPosition(true, node.X, node.Y, node.Z, GetProbableTheta(node), 0f);
        CurrentNode = node;
    }

    private float GetProbableTheta(GraphNode graphNode)
    {
        var neighbor = AgvGraph?.Edges.FirstOrDefault(e => e.ToNodeId == graphNode.Id || e.FromNodeId == graphNode.Id);
        if (neighbor == null)
        {
            return InitialTheta;
        }
        var otherNode = neighbor.FromNodeId == graphNode.Id ? AgvGraph!.Nodes.FirstOrDefault(n => n.Id == neighbor.ToNodeId) : AgvGraph!.Nodes.FirstOrDefault(n => n.Id == neighbor.FromNodeId);

        if (otherNode == null)
        {
            return InitialTheta;
        }

        float theta = GetTheta(graphNode, otherNode);
        return theta;
    }

    /// <inheritdoc cref="Agv" />
    public AgvData? AgvData { get; }

    /// <summary>
    /// Current action (this is not reset after finish)
    /// </summary>
    public AgvAction? Action { get; set; }

    /// <summary>
    /// Token used by the send status update schedule
    /// </summary>
    public Guid? SendStatusUpdateTokenId { get; set; }

    /// <summary>
    /// Token used for by finish action schedule
    /// </summary>
    public Guid? FinishActionTokenId { get; set; }

    /// <summary>
    /// Token used for signal position scheduling
    /// </summary>
    public Guid? SignalPositionTokenId { get; set; }

    /// <summary>
    /// Time it takes to travel from one node to another in milliseconds
    /// </summary>
    public int NodeTravelTime { get; set; }

    /// <summary>
    /// Returns the node last visited by the AGV
    /// </summary>
    public string? LastNodeId => CurrentNode?.Id;

    /// <summary>
    /// True when the last action has been aborted
    /// </summary>
    public bool LastActionAborted { get; set; }

    /// <summary>
    /// Next node AGV will arrive at.
    /// On destination arrival this is the same as <see cref="LastNodeId"/>.
    /// </summary>
    public string? NextNodeId
    {
        get
        {
            string? lastNodeId = LastNodeId; // Access time dependent value just once
            if (Action != null && lastNodeId != null && lastNodeId != Action!.Nodes.Last())
            {
                return Action!.Nodes[Array.IndexOf(Action!.Nodes, lastNodeId) + 1];
            }
            return lastNodeId;
        }
    }

    /// <summary>
    /// Return the current position based on the time passed traveling.
    /// </summary>
    public AgvPosition? Position { get; private set; }

    private static AgvPosition AddNoise(AgvPosition agvPosition)
    {
        return new AgvPosition(agvPosition.PositionInitialized,
            agvPosition.X + GetRandomNoise(0.005),
            agvPosition.Y + GetRandomNoise(0.005),
            agvPosition.Z + GetRandomNoise(0.001),
            agvPosition.Theta,
            agvPosition.DeviationRange);
    }

    private static float GetRandomNoise(double maxValue) => (float) (Random.Shared.NextDouble() * maxValue);

    /// <summary>
    /// Time the action processing started (set to null when finished)
    /// </summary>
    public DateTime? ActionStartDate { get; set; }

    /// <summary>
    /// Time the action takes to complete
    /// </summary>
    public TimeSpan? ActionDuration
    {
        get
        {
            if (Action == null)
            {
                return null;
            }
            if (Action.ActionType == ActionType.Charge)
            {
                double chargeDiff = 100 - SimulatedBatteryCharge;
                if (chargeDiff <= 0)
                {
                    return TimeSpan.Zero;
                }
                return TimeSpan.FromSeconds(chargeDiff / BatteryChargeRate);
            }
            return TimeSpan.FromMilliseconds((Action!.Nodes.Length - 1 + 0.001) * NodeTravelTime);
        }
    }

    /// <summary>
    /// Time the order will be finished
    /// This value is calculated base on OrderStartDate and the number of nodes
    /// </summary>
    public DateTime? ActionFinishDate => ActionStartDate != null ? ActionStartDate + ActionDuration : null;

    /// <summary>
    /// Remaining time until the action is finished.
    /// This values is calculated based on the current time and ActionFinishDate
    /// </summary>
    public TimeSpan? TimeToFinish => ActionFinishDate != null ? ActionFinishDate - _timeProvider.Now() : null;

    /// <summary>
    /// Action progress as percentage [0..1]
    /// </summary>
    public double? ActionProgress => TimeToFinish != null && ActionDuration != null ? Math.Clamp(1 - TimeToFinish.Value.TotalSeconds / ActionDuration.Value.TotalSeconds, 0, 1) : null;

    /// <summary>
    /// Simulated Agv errors
    /// </summary>
    public List<AgvError> Errors { get; set; } = new();

    /// <summary>
    /// SimulatedAgvSaga constructor used for testing
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="agvId"></param>
    /// <param name="nodeTravelTime"></param>
    /// <param name="initialBatteryCharge"></param>
    /// <param name="batteryChargeRate"></param>
    /// <param name="timeProvider"></param>
    /// <param name="nodes"></param>
    /// <param name="currentState"></param>
    /// <param name="agvGraph"></param>
    /// <param name="agvData"></param>
    /// <param name="currentNode"></param>
    /// <param name="errors"></param>
    public SimulatedAgvSaga(
        Guid correlationId,
        string agvId,
        int nodeTravelTime,
        int initialBatteryCharge,
        double batteryChargeRate,
        ITimeProvider timeProvider,
        string[]? nodes = null,
        string? currentState = null,
        AgvGraph? agvGraph = null,
        AgvData? agvData = null,
        GraphNode? currentNode = null,
        List<AgvError>? errors = null
    )
    {
        if (correlationId == Guid.Empty)
        {
            throw new InvalidOperationException($"{GetType()} requires correlation ID.");
        }
        BatteryChargeRate = batteryChargeRate;
        _timeProvider = timeProvider;
        CorrelationId = correlationId;
        AgvId = agvId;
        NodeTravelTime = nodeTravelTime;
        CurrentState = currentState ?? "";
        CurrentNode = currentNode;
        AgvGraph = agvGraph;
        AgvData = agvData;
        Errors = errors ?? new List<AgvError>();

        AgvStatus = new AgvStatus(
            CorrelationId,
            GetOrderId(),
            false,
            true,
            initialBatteryCharge,
            false,
            false,
            LastNodeId
        );
    }

    /// <summary>
    /// SimulatedAgvSaga constructor used by state machine
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="nodeTravelTime"></param>
    /// <param name="initialBatteryCharge"></param>
    /// <param name="batteryChargeRate"></param>
    /// <param name="timeProvider"></param>
    /// <param name="currentNode"></param>
    public SimulatedAgvSaga(SimulatedAgv agv, int nodeTravelTime, int initialBatteryCharge, double batteryChargeRate, ITimeProvider timeProvider, GraphNode? currentNode = null)
    {
        _timeProvider = timeProvider;
        CorrelationId = agv.CorrelationId;
        AgvId = agv.AgvId;
        CurrentNode = currentNode;
        AgvGraph = agv.AgvGraph;
        AgvData = agv.AgvData;
        CurrentState = agv.CurrentState;
        NodeTravelTime = nodeTravelTime;
        SimulatedBatteryCharge = initialBatteryCharge;
        BatteryChargeRate = batteryChargeRate;

        AgvStatus = new AgvStatus(
            CorrelationId,
            GetOrderId(),
            false,
            true,
            (int) SimulatedBatteryCharge,
            false,
            false,
            LastNodeId
        );
    }

    private string? GetOrderId() => Action?.CorrelationId.ToString();

    /// <summary>
    /// Reset action
    /// </summary>
    public void ResetAction()
    {
        ActionStartDate = null;
        // Do not reset Action to provide information such as last node
    }

    /// <summary>
    /// Updates the AgvPosition and CurrentNode of the AGV based on the time passed traveling.
    /// </summary>
    public void UpdatePosition()
    {
        if (AgvGraph == null || ActionProgress == null || Action == null)
        {
            return;
        }
        // Access time dependent value just once
        double progress = (double) ActionProgress;
        if (double.IsNaN(progress))
        {
            // in case we are at the target of the action already progress will be become NaN
            progress = 1;
        }

        string[] actionNodes = Action.Nodes;
        int currentNodeIndex = (int) (actionNodes.Length * progress);
        if (currentNodeIndex >= actionNodes.Length)
        {
            // This happens when accessing position when or after order has finished
            currentNodeIndex = actionNodes.Length - 1;
        }

        var currentNode = AgvGraph.Nodes.First(n => n.Id == actionNodes[currentNodeIndex]);
        CurrentNode = currentNode;
        var currentVector = currentNode.AsVector3();

        float x = currentNode.X;
        float y = currentNode.Y;
        float z = currentNode.Z;
        float theta = Position?.Theta ?? GetProbableTheta(currentNode);
        if (currentNodeIndex < actionNodes.Length - 1)
        {
            double partialProgress = (progress - currentNodeIndex / (double) actionNodes.Length) * actionNodes.Length;
            var nextNode = AgvGraph.Nodes.First(n => n.Id == actionNodes[currentNodeIndex + 1]);
            var nextVector = nextNode.AsVector3();
            theta = GetTheta(currentVector, nextVector);
            var currentToNextVector = nextVector - currentVector;
            currentToNextVector *= (float) partialProgress;
            currentToNextVector = currentVector + currentToNextVector;
            x = currentToNextVector.X;
            y = currentToNextVector.Y;
            z = currentToNextVector.Z;
        }
        else
        {
            // we are on the last node
            if (currentNodeIndex > 0)
            {
                // so look back
                var previousNode = AgvGraph.Nodes.First(n => n.Id == actionNodes[currentNodeIndex - 1]);
                var previousVector = previousNode.AsVector3();
                theta = GetTheta(previousVector, currentVector);
            }
            // otherwise we are already at the target node
        }

        Position = AddNoise(new AgvPosition(true, x, y, z, theta, 0f));
    }

    private static float GetTheta(GraphNode fromNode, GraphNode toNode) => GetTheta(fromNode.AsVector3(), toNode.AsVector3());

    private static float GetTheta(Vector3 fromVector, Vector3 toVector) => (float) (Math.Atan2(toVector.Y - fromVector.Y, toVector.X - fromVector.X) + Math.PI / 2);

    /// <summary>
    /// Abort action
    /// </summary>
    public void AbortAction()
    {
        ActionStartDate = null;
        // Do not reset Action to provide information such as last node
        LastActionAborted = true;
    }

    /// <summary>
    /// Updates AgvStatusReceived
    /// </summary>
    public void UpdateStatus(double timeDiffMs, double batteryChargeRate, double batteryDischargeRate)
    {
        bool charging = false;
        if (CurrentNode != null && Action != null) // AGV is charging if it is at a charging station
        {
            charging = ActionStartDate != null && CurrentNode.Type == AgvGraph.NodeType.Charge && AgvStatus.BatteryCharge < 100 && Action.ActionType == ActionType.Charge;
        }

        double chargeDiff;
        if (charging)
        {
            chargeDiff = timeDiffMs * batteryChargeRate;
        }
        else
        {
            chargeDiff = timeDiffMs * -batteryDischargeRate;
        }

        bool busy = new[]
        {
            nameof(SimulatedAgvStateMachine.BusyMoving)
        }.Any(c => c == CurrentState);

        SimulatedBatteryCharge = Math.Clamp(SimulatedBatteryCharge + chargeDiff, 0, 100);
        IsOnline = SimulatedBatteryCharge > AliveChargeThreshold;
        UpdatePosition();

        AgvStatus = new AgvStatus(
            AgvStatus.CorrelationId,
            GetOrderId(),
            busy,
            AgvStatus.Automatic,
            SimulatedBatteryCharge,
            charging,
            AgvStatus.Disturbed,
            LastNodeId
        );
    }
}