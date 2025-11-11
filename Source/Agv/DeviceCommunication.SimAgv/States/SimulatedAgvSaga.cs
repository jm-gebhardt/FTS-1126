using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Messages.ActionGroups;
using Gebhardt.Acc.Common.Messages.Contracts;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Extensions;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Travel;
using JetBrains.Annotations;
using MassTransit;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.States;

/// <summary>
/// Agv Saga for simulated AGVs
/// </summary>
[PublicAPI]
public class SimulatedAgvSaga : SagaStateMachineInstance, IAgv
{
    private readonly TimeProvider _timeProvider;

    private const float InitialTheta = 0f;

    /// <summary>
    /// Threshold [%] for battery charge to be considered alive.
    /// </summary>
    public const int AliveChargeThreshold = 5;

    /// <summary>
    /// Maximum possible battery charge [%].
    /// </summary>
    public const double MaxCharge = 100;

    /// <summary>
    /// True if the agv has been removed from all zones.
    /// </summary>
    public bool AgvRemovedFromZones { get; set; }

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
    /// Node ids where AGVs initially spawn.
    /// </summary>
    public string[]? SpawnNodeIds { get; set; }

    /// <inheritdoc cref="IAgv.CorrelationId" />
    public Guid CorrelationId { get; set; }

    /// <inheritdoc/>
    public string SerialNumber { get; }

    /// <summary>
    /// The current state of the agv.
    /// </summary>
    public string CurrentState { get; set; }

    /// <summary>
    /// Returns true when there is a sign of life of the agv in regular time intervals,
    /// returns false when there is no sign of life of the agv for a certain time.
    /// </summary>
    public bool IsOnline { get; set; } = true;

    /// <summary>
    /// Is the agv locked?
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// Returns true if the agv has any error and or is not ready to drive.
    /// </summary>
    public bool HasError { get; }

    /// <summary>
    /// Returns true if the graph of this agv exists.
    /// </summary>
    public bool GraphExists { get; set; }

    /// <summary>
    /// True, if the action should fail instead of complete.
    /// </summary>
    public bool ShouldFail { get; set; }

    /// <summary>
    /// Gets the time of the last state transition.
    /// </summary>
    public DateTimeOffset TimeOfStateTransition { get; set; }

    /// <summary>
    /// Gets the time of the last lock state change.
    /// </summary>
    public DateTimeOffset TimeOfLockStateChanged { get; set; }

    /// <summary>
    /// Gets the commissioning date of the agv.
    /// </summary>
    public DateTimeOffset CommissioningDate { get; set; }

    /// <summary>
    /// Gets the total distance driven by the agv since commissioning.
    /// </summary>
    public float TotalDistance { get; set; }

    /// <summary>
    /// Last received agv status.
    /// </summary>
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

    private ICollection<AgvZone> _agvZones;

    /// <summary>
    /// The agv zones.
    /// </summary>
    public ICollection<AgvZone> AgvZones
    {
        get => _agvZones;
        set
        {
            _agvZones = value;
            GetZones();
        }
    }

    /// <summary>
    /// Create zone around a charger or parker.
    /// </summary>
    private void GetZones()
    {
        if (_agvGraph != null && !_agvGraph.Nodes.IsNullOrEmpty() && _agvZones.IsNullOrEmpty())
        {
            var singleAgvNode = _agvGraph.Nodes.FirstOrDefault(n => n.IsCharger() || n.IsParker());
            if (singleAgvNode == null)
            {
                return;
            }
            var zonePoints = new List<PolygonPoint> { new(singleAgvNode.X - 1, singleAgvNode.Y), new(singleAgvNode.X, singleAgvNode.Y - 1), new(singleAgvNode.X + 1, singleAgvNode.Y), new(singleAgvNode.X, singleAgvNode.Y + 1) };

            var agvZone = new AgvZone(singleAgvNode.Name, 1, zonePoints, []);
            _agvZones = [agvZone];
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

        List<GraphNode> spawnGraphNodes = graphNodes.Where(n => SpawnNodeIds != null && SpawnNodeIds.Contains(n.Name)).ToList();
        if (SpawnNodeIds is { Length: > 0 } && spawnGraphNodes.Count != SpawnNodeIds.Length)
        {
            string nodeIdsString = string.Join(",", SpawnNodeIds);
            throw new ArgumentException($"Option '--SpawnAt' contains unknown node id(s). Check [{nodeIdsString}].");
        }
        // if we have no stations, we just pick a random node
        var stationNode = stations.Count == 0 ? graphNodes.ElementAt(Random.Shared.Next(0, graphNodes.Count)) : stations[Random.Shared.Next(0, stations.Count)];
        // but we prefer spawnGraphNodes if available
        var node = spawnGraphNodes.Count == 0 ? stationNode : spawnGraphNodes[Random.Shared.Next(0, spawnGraphNodes.Count)];

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

    /// <summary>
    /// The data of the agv.
    /// </summary>
    public AgvData? AgvData { get; set; }

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
    public Guid? LastNodeId => CurrentNode?.Id;

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
    public DateTimeOffset? ActionStartDate { get; set; }

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
    public DateTimeOffset? ActionFinishDate => ActionStartDate != null ? ActionStartDate.Value + ActionDuration + TravelSimulator.PauseTime : null;

    /// <summary>
    /// Remaining duration of the action. Keeps running when the drive is paused, e.g. due to an error.
    /// </summary>
    public TimeSpan? RemainingDuration => ActionFinishDate - _timeProvider.GetUtcNow();

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
    /// <param name="spawnNodeIds"></param>
    public SimulatedAgvSaga(
        Guid correlationId,
        string serialNumber,
        int simulationSpeed,
        int initialBatteryCharge,
        double batteryChargeRate,
        TimeProvider timeProvider,
        string? currentState = null,
        AgvGraph? agvGraph = null,
        AgvData? agvData = null,
        GraphNode? currentNode = null,
        List<AgvError>? errors = null,
        string[]? spawnNodeIds = null
    )
    {
        if (correlationId == Guid.Empty)
        {
            throw new InvalidOperationException($"{GetType()} requires correlation ID.");
        }
        BatteryChargeRate = batteryChargeRate;
        _timeProvider = timeProvider;
        TimeOfStateTransition = _timeProvider.GetUtcNow();
        TimeOfLockStateChanged = _timeProvider.GetUtcNow();
        CorrelationId = correlationId;
        SerialNumber = serialNumber;
        SetSimulationSpeed(simulationSpeed);
        CurrentState = currentState ?? "";
        CurrentNode = currentNode;
        SpawnNodeIds = spawnNodeIds;
        AgvGraph = agvGraph;
        if (agvData != null)
        {
            agvData.SetSimulationSpeed(simulationSpeed);
        }
        AgvData = agvData;
        Errors = errors ?? new List<AgvError>();
        _agvZones = [];

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
        TravelSimulator = new AgvTravelSimulator(agvGraph, Position!, _timeProvider);
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
    /// <param name="spawnNodeIds"></param>
    public SimulatedAgvSaga(SimulatedAgv agv, int simulationSpeed, int initialBatteryCharge, double batteryChargeRate, TimeProvider timeProvider, GraphNode? currentNode = null, string[]? spawnNodeIds = null)
    {
        _timeProvider = timeProvider;
        CorrelationId = agv.CorrelationId;
        SerialNumber = agv.SerialNumber;
        CommissioningDate = agv.CommissioningDate;
        CurrentNode = currentNode;
        SpawnNodeIds = spawnNodeIds;
        AgvGraph = agv.AgvGraph;
        AgvData = agv.AgvData;
        SetSimulationSpeed(simulationSpeed);
        CurrentState = agv.CurrentState;
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
        TravelSimulator = new AgvTravelSimulator(agv.AgvGraph, Position!, _timeProvider);
        _agvZones = [];
    }

    private static float GetRandomNoise(double maxValue) => (float) (maxValue * (2 * Random.Shared.NextDouble() - 1));

    private string? GetOrderId() => Action?.CorrelationId.ToString();

    private static float GetBatteryVoltage() => 48 + GetRandomNoise(0.1);

    private static double GetBatteryTemperature() => 20 + GetRandomNoise(0.1);

    private DateTime? GetEstimatedFullyChargedTime(bool charging) => charging ? _timeProvider.GetUtcNow().AddSeconds((MaxCharge - SimulatedBatteryCharge) / BatteryChargeRate).UtcDateTime : null;

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
    /// Sets the simulation speed for the AGV.
    /// </summary>
    /// <param name="simulationSpeed">The factor applied to agv velocities and acceleration to control the speed of the simulation.</param>
    public void SetSimulationSpeed(int simulationSpeed)
    {
        SimulationSpeed = simulationSpeed;
        if (AgvData != null)
        {
            AgvData = AgvData!.SetSimulationSpeed(simulationSpeed);
        }
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
            TravelSimulator.UpdatePosition(AgvData!);
            CurrentNode = TravelSimulator.CurrentNode!;
            Position = TravelSimulator.Position;
            TotalDistance += TravelSimulator.CurrentEdgeDistance;
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
            charging = ActionStartDate != null && CurrentNode.IsCharger() && AgvStatus.BatteryCharge < MaxCharge && Action.ActionType == ActionType.Charge;
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

    private ICollection<string>? GetZoneIds()
    {
        if (Position == null)
        {
            return null;
        }
        return AgvZones.Where(z => z.Polygon.PointIsInPolygon(Position.X, Position.Y)).Select(z => z.ZoneId).ToList();
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
            GetEstimatedFullyChargedTime(charging),
            null,
            AgvRemovedFromZones ? null : GetZoneIds(),
            null,
            TotalDistance,
            (float) (_timeProvider.GetUtcNow() - CommissioningDate).TotalHours
        );
    }
}