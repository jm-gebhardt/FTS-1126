using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.States;

/// <summary>
/// Agv Saga for simulated AGVs
/// </summary>
[PublicAPI]
public class SimulatedAgvSaga : SagaStateMachineInstance, IAgv
{
    private const int AliveChargeThreshold = 5;

    private AgvPosition? _position;

    /// <summary>
    /// Value for simulating change in battery charge
    /// </summary>
    public double SimulatedBatteryCharge { get; set; }

    /// <summary>
    /// Current node of this AGV
    /// </summary>
    public GraphNode? CurrentNode { get; set; }

    /// <summary>
    /// Correlation ID
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <inheritdoc cref="IAgv" />
    public string AgvId { get; }

    /// <inheritdoc cref="IAgv" />
    public string CurrentState { get; set; }

    /// <inheritdoc cref="IAgv" />
    public bool IsOnline { get; set; } = true;

    /// <inheritdoc />
    public bool IsLocked { get; set; }

    /// <inheritdoc />
    public bool HasError { get; }

    /// <inheritdoc />
    public bool GraphExists { get; set; }

    /// <inheritdoc />
    public DateTime TimeOfStateTransition { get; set; } = DateTime.UtcNow;

    /// <inheritdoc />
    public DateTime TimeOfLockStateChanged { get; set; } = DateTime.UtcNow;

    /// <inheritdoc cref="IAgv" />
    public AgvStatus AgvStatus { get; private set; }

    /// <summary>
    /// We still save the graph in the simulated Agv for convenience
    /// </summary>
    public AgvGraph? AgvGraph { get; }

    /// <inheritdoc cref="IAgv" />
    public AgvData? AgvData { get; }

    /// <summary>
    /// Returns the map according to <see cref="AgvGraph"/>.
    /// </summary>
    public AgvMap? AgvMap { get; }

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
    public AgvPosition? Position
    {
        get
        {
            if (AgvGraph == null)
            {
                _position = null;
                CurrentNode = null;
                return _position;
            }

            double? orderProgress = ActionProgress; // Access time dependent value just once

            if (!orderProgress.HasValue) // Use first node as initial position
            {
                if (_position == null)
                {
                    // Init position to random station node
                    List<GraphNode> stations = AgvGraph.Nodes.Where(n =>
                    {
                        switch (n.Type)
                        {
                            case AgvGraph.NodeType.Pick: return true;
                            case AgvGraph.NodeType.Drop: return true;
                            case AgvGraph.NodeType.PickDrop: return true;
                            default: return false;
                        }
                    }).ToList();

                    var rnd = new Random();
                    var node = stations[rnd.Next(0, stations.Count)];
                    _position = new AgvPosition(true, node.X, node.Y, node.Z, 0f, 0f);
                    CurrentNode = node;
                }
                return _position;
            }

            int lastNodeIndex = (int) (Action!.Nodes.Length * orderProgress.Value);
            if (lastNodeIndex >= Action!.Nodes.Length)
            {
                // This happens when accessing position when or after order has finished
                lastNodeIndex = Action!.Nodes.Length - 1;
            }

            var lastNode = AgvGraph!.Nodes.First(x => x.Id == Action!.Nodes[lastNodeIndex]);
            if (lastNodeIndex < Action!.Nodes.Length - 1)
            {
                double partialProgress = (orderProgress.Value - lastNodeIndex / (double) Action!.Nodes.Length) * Action!.Nodes.Length;
                var nextNode = AgvGraph!.Nodes.First(x => x.Id == Action!.Nodes[lastNodeIndex + 1]);
                var vectorLast = new Vector3(lastNode.X, lastNode.Y, lastNode.Z);
                var vectorNext = new Vector3(nextNode.X, nextNode.Y, nextNode.Z);
                float theta = (float) (Math.Atan2(vectorNext.Y - vectorLast.Y, vectorNext.X - vectorLast.X) + Math.PI / 2);
                var vectorEdge = vectorNext - vectorLast;
                vectorEdge *= (float) partialProgress;
                vectorEdge = vectorLast + vectorEdge;

                _position = new AgvPosition(true, vectorEdge.X, vectorEdge.Y, vectorEdge.Z, theta, 0f);
            }
            else
            {
                _position = new AgvPosition(true, lastNode.X, lastNode.Y, lastNode.Z, 0f, 0f);
            }

            CurrentNode = lastNode;

            return _position;
        }
    }

    /// <summary>
    /// Time the action processing started (set to null when finished)
    /// </summary>
    public DateTime? ActionStartDate { get; set; }

    /// <summary>
    /// Time the action takes to complete
    /// </summary>
    public TimeSpan? ActionDuration => TimeSpan.FromMilliseconds((Action!.Nodes.Length - 1) * NodeTravelTime);

    /// <summary>
    /// Time the order will be finished
    /// This value is calculated base on OrderStartDate and the number of nodes
    /// </summary>
    public DateTime? ActionFinishDate => ActionStartDate != null ? ActionStartDate + ActionDuration : null;

    /// <summary>
    /// Remaining time until the order is finished.
    /// This values is calculated based on the current time and OrderFinishDate
    /// </summary>
    public TimeSpan? TimeToFinish => ActionFinishDate != null ? ActionFinishDate - DateTime.UtcNow : null;

    /// <summary>
    /// Action progress as percentage [0..1]
    /// </summary>
    public double? ActionProgress => TimeToFinish != null && ActionDuration != null ? Math.Clamp(1 - TimeToFinish.Value.TotalSeconds / ActionDuration.Value.TotalSeconds, 0, 1) : null;

    /// <summary>
    /// SimulatedAgvSaga constructor used for testing
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="agvId"></param>
    /// <param name="nodeTravelTime"></param>
    /// <param name="initialBatteryCharge"></param>
    /// <param name="nodes"></param>
    /// <param name="currentState"></param>
    /// <param name="agvGraph"></param>
    /// <param name="agvData"></param>
    /// <param name="agvMap"></param>
    public SimulatedAgvSaga(
        Guid correlationId,
        string agvId,
        int nodeTravelTime,
        int initialBatteryCharge,
        string[]? nodes = null,
        string? currentState = null,
        AgvGraph? agvGraph = null,
        AgvData? agvData = null,
        AgvMap? agvMap = null)
    {
        if (correlationId == Guid.Empty)
        {
            throw new InvalidOperationException($"{GetType()} requires correlation ID.");
        }
        CorrelationId = correlationId;
        AgvId = agvId;
        NodeTravelTime = nodeTravelTime;
        CurrentState = currentState ?? "";
        AgvGraph = agvGraph;
        AgvData = agvData;
        AgvMap = agvMap;

        AgvStatus = new(
            CorrelationId,
            GetOrderId(),
            busy: false,
            automatic: true,
            initialBatteryCharge,
            charging: false,
            disturbed: false,
            LastNodeId
        );
    }

    private string? GetOrderId() => Action?.CorrelationId.ToString();

    /// <summary>
    /// SimulatedAgvSaga constructor used by state machine
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="nodeTravelTime"></param>
    /// <param name="initialBatteryCharge"></param>
    public SimulatedAgvSaga(SimulatedAgv agv, int nodeTravelTime, int initialBatteryCharge)
    {
        CorrelationId = agv.CorrelationId;
        AgvId = agv.AgvId;
        AgvGraph = agv.AgvGraph;
        AgvData = agv.AgvData;
        CurrentState = agv.CurrentState;
        AgvMap = agv.AgvMap;
        NodeTravelTime = nodeTravelTime;
        SimulatedBatteryCharge = initialBatteryCharge;

        AgvStatus = new(
            CorrelationId,
            GetOrderId(),
            busy: false,
            automatic: true,
            (int) SimulatedBatteryCharge,
            charging: false,
            disturbed: false,
            LastNodeId
        );
    }

    /// <summary>
    /// Reset action
    /// </summary>
    public void ResetAction()
    {
        ActionStartDate = null;
        // Do not reset Action to provide information such as last node
    }

    /// <summary>
    /// Updates AgvStatusReceived
    /// </summary>
    public void UpdateStatus(double timeDiffMs, double batteryChargeRate, double batteryDischargeRate)
    {
        bool charging = false;
        if (CurrentNode != null) // AGV is charging if it is at a charging station
        {
            charging = CurrentNode.Type == AgvGraph.NodeType.Charge && AgvStatus.BatteryCharge < 100;
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

        AgvStatus = new(
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