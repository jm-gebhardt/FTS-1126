using Gebhardt.StoreWare.Agv.Common.Contracts;
using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.ActionGroups;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Time;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Extensions;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation;
using Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.States;

/// <summary>
/// Agv Saga for simulated AGVs
/// </summary>
[PublicAPI]
public class SimulatedAgvSaga : SagaStateMachineInstance, IAgv
{
    private readonly ITimeProvider _timeProvider;

    private const float InitialTheta = 0f;

    /// <summary>
    /// Threshold [%] for battery charge to be considered alive.
    /// </summary>
    public const int AliveChargeThreshold = 5;

    /// <summary>
    /// Maximum possible battery charge [%].
    /// </summary>
    public const double MaxCharge = 100;

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

    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; set; }

    /// <inheritdoc/>
    public string SerialNumber { get; }

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

        return graphNode.IsStation() ? BaseEdge.GetAgvOrientation(otherNode, graphNode) : BaseEdge.GetAgvOrientation(graphNode, otherNode);
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
    /// The factor applied to agv velocities and acceleration to control the simulation speed.
    /// </summary>
    public int SimulationSpeed { get; set; }

    /// <summary>
    /// Returns the node last visited by the AGV
    /// </summary>
    public string? LastNodeId => CurrentNode?.Id;

    /// <summary>
    /// True when the last action has been aborted
    /// </summary>
    public bool LastActionAborted { get; set; }

    /// <summary>
    /// Return the current position based on the time passed traveling.
    /// </summary>
    public AgvPosition? Position { get; private set; }

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
                double chargeDiff = MaxCharge - SimulatedBatteryCharge;
                if (chargeDiff <= 0)
                {
                    return TimeSpan.Zero;
                }
                return TimeSpan.FromSeconds(chargeDiff / BatteryChargeRate);
            }
            return TravelSimulator.TravelDuration;
        }
    }

    /// <summary>
    /// Time the order will be finished
    /// This value is calculated base on OrderStartDate and the number of nodes. Keeps running when the drive is paused, e.g. due to an error.
    /// </summary>
    public DateTime? ActionFinishDate => ActionStartDate != null ? ActionStartDate + ActionDuration + TravelSimulator.PauseTime : null;

    /// <summary>
    /// Remaining duration of the action. Keeps running when the drive is paused, e.g. due to an error.
    /// </summary>
    public TimeSpan? RemainingDuration => ActionFinishDate - _timeProvider.Now();

    /// <summary>
    /// Simulated Agv errors
    /// </summary>
    public List<AgvError> Errors { get; set; } = new();

    /// <summary>
    /// The agv travel simulator.
    /// </summary>
    public IAgvTravelSimulator TravelSimulator { get; set; }

    /// <summary>
    /// SimulatedAgvSaga constructor used for testing
    /// </summary>
    /// <param name="correlationId"></param>
    /// <param name="serialNumber"></param>
    /// <param name="simulationSpeed"></param>
    /// <param name="initialBatteryCharge"></param>
    /// <param name="batteryChargeRate"></param>
    /// <param name="timeProvider"></param>
    /// <param name="currentState"></param>
    /// <param name="agvGraph"></param>
    /// <param name="agvData"></param>
    /// <param name="currentNode"></param>
    /// <param name="errors"></param>
    public SimulatedAgvSaga(
        Guid correlationId,
        string serialNumber,
        int simulationSpeed,
        int initialBatteryCharge,
        double batteryChargeRate,
        ITimeProvider timeProvider,
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
        SerialNumber = serialNumber;
        SimulationSpeed = simulationSpeed;
        CurrentState = currentState ?? "";
        CurrentNode = currentNode;
        AgvGraph = agvGraph;
        if (agvData != null)
        {
            agvData.SetSimulationSpeed(simulationSpeed);
        }
        AgvData = agvData;
        Errors = errors ?? new List<AgvError>();

        const bool charging = false;
        AgvStatus = new AgvStatus(
            CorrelationId,
            GetOrderId(),
            false,
            true,
            initialBatteryCharge,
            charging,
            GetBatteryVoltage(),
            GetBatteryCurrent(),
            GetBatteryTemperature(),
            false,
            LastNodeId,
            GetEstimatedFullyChargedTime(charging)
        );
        TravelSimulator = new AgvTravelSimulator(agvGraph, agvData!, Position!, _timeProvider);
    }

    /// <summary>
    /// SimulatedAgvSaga constructor used by state machine
    /// </summary>
    /// <param name="agv"></param>
    /// <param name="simulationSpeed"></param>
    /// <param name="initialBatteryCharge"></param>
    /// <param name="batteryChargeRate"></param>
    /// <param name="timeProvider"></param>
    /// <param name="currentNode"></param>
    public SimulatedAgvSaga(SimulatedAgv agv, int simulationSpeed, int initialBatteryCharge, double batteryChargeRate, ITimeProvider timeProvider, GraphNode? currentNode = null)
    {
        _timeProvider = timeProvider;
        CorrelationId = agv.CorrelationId;
        SerialNumber = agv.SerialNumber;
        CurrentNode = currentNode;
        AgvGraph = agv.AgvGraph;
        var agvData = agv.AgvData;
        if (agv.AgvData != null)
        {
            agvData = agv.AgvData!.SetSimulationSpeed(simulationSpeed);
        }
        AgvData = agvData;
        CurrentState = agv.CurrentState;
        SimulationSpeed = simulationSpeed;
        SimulatedBatteryCharge = initialBatteryCharge;
        BatteryChargeRate = batteryChargeRate;

        const bool charging = false;
        AgvStatus = new AgvStatus(
            CorrelationId,
            GetOrderId(),
            false,
            true,
            (int) SimulatedBatteryCharge,
            charging,
            GetBatteryVoltage(),
            GetBatteryCurrent(),
            GetBatteryTemperature(),
            false,
            LastNodeId,
            GetEstimatedFullyChargedTime(charging)
        );
        TravelSimulator = new AgvTravelSimulator(agv.AgvGraph, agvData!, Position!, _timeProvider);
    }

    private static float GetRandomNoise(double maxValue) => (float) (maxValue * (2 * Random.Shared.NextDouble() - 1));

    private string? GetOrderId() => Action?.CorrelationId.ToString();

    private static float GetBatteryVoltage() => 48 + GetRandomNoise(0.1);

    private static double GetBatteryTemperature() => 20 + GetRandomNoise(0.1);

    private DateTime? GetEstimatedFullyChargedTime(bool charging) => charging ? _timeProvider.Now().AddSeconds((MaxCharge - SimulatedBatteryCharge) / BatteryChargeRate) : null;

    private double GetBatteryCurrent()
    {
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        // for some reason it is actually null at startup
        if (AgvStatus?.Charging ?? false)
        {
            return 50 + GetRandomNoise(1);
        }

        if (AgvStatus?.Busy ?? false)
        {
            return -20 + GetRandomNoise(5);
        }

        return -2 + GetRandomNoise(0.1);
    }

    /// <summary>
    /// Reset action
    /// </summary>
    public void ResetAction()
    {
        ActionStartDate = null;
        // Do not reset Action to provide information such as last node
        TravelSimulator.FinishTravel();
    }

    /// <summary>
    /// Updates the AgvPosition and CurrentNode of the AGV based on the simulated agv travel.
    /// </summary>
    public void UpdatePosition()
    {
        if (Action != null && TravelSimulator.CurrentEdge != null!)
        {
            TravelSimulator.UpdatePosition();
            CurrentNode = TravelSimulator.CurrentNode!;
            Position = TravelSimulator.Position;
        }
    }

    /// <summary>
    /// Abort action
    /// </summary>
    public void AbortAction()
    {
        ActionStartDate = null;
        // Do not reset Action to provide information such as last node
        LastActionAborted = true;
        TravelSimulator.FinishTravel();
    }

    /// <summary>
    /// Updates AgvStatusReceived
    /// </summary>
    public void UpdateStatus(double timeDiffS, double batteryChargeRate, double batteryDischargeRate)
    {
        bool charging = false;
        if (CurrentNode != null && Action != null) // AGV is charging if it is at a charging station
        {
            charging = ActionStartDate != null && CurrentNode.Type == AgvGraph.NodeType.Charge && AgvStatus.BatteryCharge < MaxCharge && Action.ActionType == ActionType.Charge;
        }

        double chargeDiff;
        if (charging)
        {
            chargeDiff = timeDiffS * batteryChargeRate;
        }
        else
        {
            chargeDiff = timeDiffS * -batteryDischargeRate;
        }

        SimulatedBatteryCharge = Math.Clamp(SimulatedBatteryCharge + chargeDiff, 0, MaxCharge);
        bool wasOnline = IsOnline;
        IsOnline = SimulatedBatteryCharge > AliveChargeThreshold;
        switch (wasOnline)
        {
            case false when IsOnline:
                TravelSimulator.Resume();
                break;

            case true when !IsOnline:
                TravelSimulator.Pause();
                break;
        }
        UpdatePosition();
        SetAgvStatus(charging);
    }

    /// <summary>
    /// Set agv battery to given level.
    /// </summary>
    /// <param name="batteryLevel">The battery level.</param>
    public void SetBatteryLevel(double batteryLevel)
    {
        SimulatedBatteryCharge = Math.Clamp(batteryLevel, 0, MaxCharge);
        bool wasOnline = IsOnline;
        IsOnline = SimulatedBatteryCharge > AliveChargeThreshold;
        if (!wasOnline && IsOnline)
        {
            TravelSimulator.Resume();
        }
        UpdatePosition();
        SetAgvStatus(false);
    }

    private void SetAgvStatus(bool charging)
    {
        bool busy = new[]
        {
            nameof(SimulatedAgvStateMachine.BusyMoving)
        }.Contains(CurrentState);
        AgvStatus = new AgvStatus(
            AgvStatus.CorrelationId,
            GetOrderId(),
            busy,
            AgvStatus.Automatic,
            SimulatedBatteryCharge,
            charging,
            GetBatteryVoltage(),
            GetBatteryCurrent(),
            GetBatteryTemperature(),
            AgvStatus.Disturbed,
            LastNodeId,
            GetEstimatedFullyChargedTime(charging)
        );
    }
}