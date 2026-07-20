using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Models.Sequence;
using Gebhardt.Acc.Common.Domain.Services;
using Gebhardt.Acc.Common.Messages.ActionGroups.Events;
using Gebhardt.Acc.Common.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Caches;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Constants;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Messages.HeaderIds;
using JetBrains.Annotations;
using MassTransit;
using Microsoft.Extensions.Logging;
using AgvPosition = Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.AgvPosition;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.States;

/// <summary>
/// The simulated agv.
/// </summary>
[PublicAPI]
public class SimulatedAgv : SagaStateMachineInstance
{
    /// <inheritdoc/>
    public Guid CorrelationId { get; set; }

    private readonly TimeProvider _timeProvider;

    private const float InitialTheta = 0f;
    private readonly HashSet<string> _processedDropActionIds = [];

    /// <summary>
    /// Threshold [%] for battery charge to be considered alive.
    /// </summary>
    public const int AliveChargeThreshold = 2;

    /// <summary>
    /// Maximum possible battery charge [%].
    /// </summary>
    public const double MaxCharge = 100;

    /// <summary>
    /// Value for simulating change in battery charge
    /// </summary>
    public double SimulatedBatteryCharge { get; set; }

    private readonly int _initialBatteryCharge;

    /// <summary>
    /// Value for simulating change in battery charge [%/s]
    /// </summary>
    public double BatteryChargeRate { get; set; }

    /// <summary>
    /// Value for simulating change in battery discharge [%/s]
    /// </summary>
    public double BatteryDischargeRate { get; set; }

    /// <summary>
    /// The agv configuration.
    /// </summary>
    public AgvConfiguration Configuration { get; }

    /// <summary>
    /// The current state of the agv.
    /// </summary>
    public string CurrentState { get; set; }

    /// <summary>
    /// True, if the agv is online.
    /// </summary>
    public bool IsOnline { get; private set; }

    /// <summary>
    /// NodeID of last reached node or, if AGV is currently on a node,
    /// current node (e.g., \"node7\"). Empty string (\"\") if no lastNodeId is available.
    /// </summary>
    public string? LastNodeId { get; set; }

    /// <summary>
    /// SequenceId of the last reached node or, if the AGV is currently on a node, sequenceId of
    /// current node.
    /// 0 if no lastNodeSequenceId is available.
    /// </summary>
    public int LastNodeSequenceId { get; set; }

    /// <summary>
    /// The state topic header id counter.
    /// </summary>
    public ISequenceCounter<IStateHeaderId> StateHeaderId { get; init; }

    /// <summary>
    /// The connection topic header id counter.
    /// </summary>
    public ISequenceCounter<IConnectionHeaderId> ConnectionHeaderId { get; init; }

    /// <summary>
    /// The visualization topic header id counter.
    /// </summary>
    public ISequenceCounter<IVisualizationHeaderId> VisualizationHeaderId { get; init; }

    /// <summary>
    /// Token used for signal position scheduling
    /// </summary>
    public Guid? SignalPositionTokenId { get; set; }

    /// <summary>
    /// Token used by the send status update schedule
    /// </summary>
    public Guid? SendStatusUpdateTokenId { get; set; }

    /// <summary>
    /// True, if the agv is charging.
    /// </summary>
    public bool Charging { get; set; }

    /// <summary>
    /// The current order.
    /// </summary>
    public Order? CurrentOrder { get; private set; }

    /// <summary>
    /// True if the current order is ready to be cleared on the next <see cref="FinishOrder"/> call.
    /// </summary>
    public bool CurrentOrderReadyToFinish => CurrentOrder?.IsCompletable(_timeProvider.GetUtcNow().DateTime) ?? false;

    /// <summary>
    /// The current agv position.
    /// </summary>
    public AgvPosition CurrentPosition { get; private set; }

    /// <summary>
    /// The loads the agv is carrying
    /// </summary>
    public ICollection<Load> Loads { get; } = [];

    /// <summary>
    /// The current operating mode.
    /// </summary>
    public AgvOperatingMode OperatingMode { get; private set; }

    /// <summary>
    /// True if the agv is in operating mode <see cref="AgvOperatingMode.Automatic"/>.
    /// </summary>
    public bool IsInAutomaticMode => OperatingMode == AgvOperatingMode.Automatic;

    /// <summary>
    /// Current error.
    /// </summary>
    public AgvError? Error { get; private set; }

    private readonly INurbsEdgeBuilder _edgeBuilder;
    private readonly bool _generateError;
    private readonly IAgvCache _agvCache;

    /// <inheritdoc cref="SimulatedAgv"/>
    public SimulatedAgv(Guid correlationId,
        AgvConfiguration configuration,
        int initialBatteryCharge,
        double batteryChargeRate,
        double batteryDischargeRate,
        TimeProvider timeProvider,
        ISequenceCounter<IStateHeaderId> stateHeaderId,
        ISequenceCounter<IVisualizationHeaderId> visualizationHeaderId,
        ISequenceCounter<IConnectionHeaderId> connectionHeaderId,
        INurbsEdgeBuilder edgeBuilder,
        IAgvCache agvCache,
        string? currentState = null,
        bool generateError = false)
    {
        CorrelationId = correlationId;
        Configuration = configuration;
        BatteryChargeRate = batteryChargeRate;
        BatteryDischargeRate = batteryDischargeRate;
        SimulatedBatteryCharge = initialBatteryCharge;
        _initialBatteryCharge = initialBatteryCharge;
        _timeProvider = timeProvider;
        CurrentState = currentState ?? string.Empty;
        IsOnline = true;
        StateHeaderId = stateHeaderId;
        VisualizationHeaderId = visualizationHeaderId;
        ConnectionHeaderId = connectionHeaderId;
        CurrentPosition = new AgvPosition(){MapId = configuration.MapId, PositionInitialized = true, Theta = InitialTheta};
        _edgeBuilder = edgeBuilder;
        _generateError = generateError;
        OperatingMode = AgvOperatingMode.Automatic;
        _agvCache = agvCache;
    }

    /// <summary>
    /// Initialize the agv to the given position.
    /// </summary>
    /// <param name="position">The new position.</param>
    /// <param name="lastNodeId">The last node id.</param>
    public void InitPosition(AgvPosition position, string? lastNodeId = null)
    {
        if (!IsInAutomaticMode)
        {
            GenerateAvailabilityError();
            return;
        }
        CurrentPosition = position;
        LastNodeId = lastNodeId;
        LastNodeSequenceId = 0;
    }

    /// <summary>
    /// Set the agv to the given position.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SetPosition(AgvPosition position)
    {
        if (IsInAutomaticMode)
        {
            GenerateAvailabilityError();
            return;
        }
        CurrentPosition = position;
        LastNodeId = null;
        LastNodeSequenceId = 0;
    }

    /// <summary>
    /// Switch the operating mode of the agv.
    /// </summary>
    /// <param name="operatingMode"></param>
    public async Task SwitchOperatingMode(AgvOperatingMode operatingMode)
    {
        await _agvCache.AddOrUpdateAsync(CorrelationId, operatingMode);
        OperatingMode = operatingMode;
        if (!IsInAutomaticMode)
        {
            if (CurrentOrder?.CurrentAction != null)
            {
                AbortOrder(CurrentOrder.CurrentAction.ActionId);
            }
            CurrentOrder = null;
            LastNodeId = null;
            LastNodeSequenceId = 0;
            return;
        }
        if (string.Equals(Error?.ErrorType, Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable))
        {
            Error = null;
        }
    }

    /// <summary>
    /// Update the agv status
    /// </summary>
    /// <param name="timeDiffS"></param>
    /// <param name="logger">Optional logger.</param>
    public void UpdateStatus(double timeDiffS, ILogger? logger = null)
    {
        double chargeDiff;
        if (Charging)
        {
            chargeDiff = timeDiffS * BatteryChargeRate;
        }
        else
        {
            chargeDiff = timeDiffS * -BatteryDischargeRate;
        }

        SimulatedBatteryCharge = Math.Clamp(SimulatedBatteryCharge + chargeDiff, 0, MaxCharge);

        if (Charging && SimulatedBatteryCharge >= MaxCharge && CurrentOrder?.CurrentAction?.ActionType == Vda5050Specifications.ActionTypes.StartCharging)
        {
            CurrentOrder.FinishCurrentChargingAction(_timeProvider.GetUtcNow().DateTime, logger);
        }

        if (_generateError)
        {
            GenerateError();
        }
    }

    /// <summary>
    /// Stops charging and finishes any running <c>startCharging</c> order action so the charge order can complete.
    /// </summary>
    public void StopCharging(ILogger? logger = null)
    {
        if (!IsInAutomaticMode)
        {
            return;
        }
        Charging = false;
        CurrentOrder?.FinishCurrentChargingAction(_timeProvider.GetUtcNow().DateTime, logger);
    }

    private void GenerateAvailabilityError()
    {
        Error = new AgvError(CorrelationId, Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable, "Receival of an order while not in ‘AUTOMATIC’", null, ErrorLevel.Warning, Vda5050Specifications.OrderWarnings.MobileRobotNotAvailable, ErrorCategory.General, null, _timeProvider.GetUtcNow().DateTime, ErrorFlag.Agv, AgvErrorSource.Agv);
    }

    /// <summary>
    /// Start a new order.
    /// </summary>
    public void StartOrder(Order order, ILogger? logger = null)
    {
        if (!IsInAutomaticMode)
        {
            GenerateAvailabilityError();
            return;
        }
        var now = _timeProvider.GetUtcNow().DateTime;
        _processedDropActionIds.Clear();
        CurrentOrder = order;
        CurrentOrder.StartTime = now;
        logger?.LogInformation("StartOrder[{OrderId} updateId {UpdateId}] previousLastNodeId={LastNodeId} baseNodes={NodeCount} baseEdges={EdgeCount}",
            order.OrderId, order.OrderUpdateId, LastNodeId ?? "(null)", order.Base.Nodes.Count, order.Base.Edges.Count);
        CurrentOrder.PrepareTravel(_edgeBuilder, Configuration.Speed, logger);
        var currentNode = CurrentOrder.GetLastNode(LastNodeId, now);
        LastNodeId = currentNode.NodeId;
        LastNodeSequenceId = currentNode.SequenceId;
        CurrentOrder.PendingActionStates =
        [
            ..CurrentOrder.Base.Nodes.SelectMany(n => n.Actions)
                .Select(a => new OrderActionState
                {
                    ActionId = a.ActionId,
                    ActionType = a.ActionType,
                    ActionDescription = a.ActionDescription,
                    ActionStatus = VehicleActionStatus.Waiting
                }),
            ..CurrentOrder.Base.Edges.SelectMany(n => n.Actions)
                .Select(a => new OrderActionState
                {
                    ActionId = a.ActionId,
                    ActionType = a.ActionType,
                    ActionDescription = a.ActionDescription,
                    ActionStatus = VehicleActionStatus.Waiting
                })
        ];
    }

    /// <summary>
    /// Update an order.
    /// </summary>
    public void UpdateOrder(Order order, ILogger? logger = null)
    {
        if (!IsInAutomaticMode)
        {
            GenerateAvailabilityError();
            return;
        }
        var now = _timeProvider.GetUtcNow().DateTime;
        var previousOrder = CurrentOrder;
        CurrentOrder = order;
        logger?.LogInformation("UpdateOrder[{OrderId} u{UpdateId}] previousLastNodeId={LastNodeId} baseNodes={NodeCount} baseEdges={EdgeCount}",
            order.OrderId, order.OrderUpdateId, LastNodeId ?? "(null)", order.Base.Nodes.Count, order.Base.Edges.Count);
        CurrentOrder.PrepareTravel(_edgeBuilder, Configuration.Speed);
        CurrentOrder.PendingActionStates =
        [
            ..CurrentOrder.Base.Nodes.SelectMany(n => n.Actions)
                .Select(a => new OrderActionState
                {
                    ActionId = a.ActionId,
                    ActionType = a.ActionType,
                    ActionDescription = a.ActionDescription,
                    ActionStatus = VehicleActionStatus.Waiting
                }),
            ..CurrentOrder.Base.Edges.SelectMany(n => n.Actions)
                .Select(a => new OrderActionState
                {
                    ActionId = a.ActionId,
                    ActionType = a.ActionType,
                    ActionDescription = a.ActionDescription,
                    ActionStatus = VehicleActionStatus.Waiting
                })
        ];

        bool continuesPreviousOrder = previousOrder?.StartTime != null && previousOrder.OrderId == order.OrderId;
        if (continuesPreviousOrder)
        {
            CurrentOrder.CarryOverProgressFrom(previousOrder!);
        }
        else
        {
            CurrentOrder.StartTime = now;
        }

        var currentNode = CurrentOrder.GetLastNode(LastNodeId, now);
        LastNodeId = currentNode.NodeId;
        LastNodeSequenceId = currentNode.SequenceId;
        CurrentOrder.CurrentRoute.Proceed(currentNode.SequenceId);
    }

    /// <summary>
    /// Proceed with the order.
    /// </summary>
    public void ProceedOrder(ILogger? logger = null)
    {
        if (CurrentOrder?.StartTime == null)
        {
            return;
        }
        var now = _timeProvider.GetUtcNow().DateTime;
        string? previousNodeId = LastNodeId;
        CurrentPosition = CurrentOrder.Proceed(LastNodeId, now, logger);
        if (CurrentOrder.State != null)
        {
            LastNodeId = CurrentOrder.Base.Nodes.First(n => n.SequenceId == CurrentOrder.State.LastNodeSequenceId).NodeId;
            LastNodeSequenceId = CurrentOrder.State.LastNodeSequenceId;
        }
        if (Charging && previousNodeId != null && previousNodeId != LastNodeId)
        {
            Charging = false;
            CurrentOrder?.FinishCurrentChargingAction(now, logger);
            logger?.LogInformation("Charging stopped: AGV {AgvId} left node {PreviousNodeId} for {LastNodeId}",
                Configuration.SerialNumber, previousNodeId, LastNodeId);
        }
        UpdateLoad(logger);
        HandleChargingNodeAction();
    }

    private void HandleChargingNodeAction()
    {
        if (CurrentOrder?.CurrentAction?.ActionType == Vda5050Specifications.ActionTypes.StartCharging)
        {
            Charging = true;
        }
        if (CurrentOrder?.CurrentAction?.ActionType == Vda5050Specifications.ActionTypes.StopCharging)
        {
            Charging = false;
        }
    }

    private void UpdateLoad(ILogger? logger = null)
    {
        if (LastNodeId == null)
        {
            logger?.LogDebug("UpdateLoad skipped: LastNodeId is null");
            return;
        }
        var pickAction = CurrentOrder?.GetActionOnNode(LastNodeId, LastNodeSequenceId, Vda5050Specifications.ActionTypes.Pick);
        bool pickFinishedHere = pickAction != null && (CurrentOrder?.PendingActionStates.Any(s => s.ActionId == pickAction.ActionId && s.ActionStatus == VehicleActionStatus.Finished) ?? false);
        if (pickFinishedHere && Loads.All(l => l.PickStartTime != pickAction!.StartedTime))
        {
            string loadId = pickAction!.ActionParameters?.SingleOrDefault(p => p.Key == Vda5050Specifications.Parameters.LoadHandlingDevice)?.Value.ToString() ?? Guid.NewGuid().ToString();
            Loads.Add(new Load(loadId, pickAction.StartedTime!.Value));
            logger?.LogInformation("UpdateLoad: added load {LoadId} for finished pick action {ActionId} at node {LastNodeId} (loads now {LoadCount})",
                loadId, pickAction.ActionId, LastNodeId, Loads.Count);
        }

        var dropAction = CurrentOrder?.GetActionOnNode(LastNodeId, LastNodeSequenceId, Vda5050Specifications.ActionTypes.Drop);
        bool dropFinishedHere = dropAction != null && (CurrentOrder?.PendingActionStates.Any(s => s.ActionId == dropAction.ActionId && s.ActionStatus == VehicleActionStatus.Finished) ?? false);
        if (dropFinishedHere && !_processedDropActionIds.Contains(dropAction!.ActionId) && Loads.Count != 0)
        {
            var load = Loads.FirstOrDefault(l => l.Id == dropAction.ActionParameters?.SingleOrDefault(p => p.Key == Vda5050Specifications.Parameters.LoadHandlingDevice)?.Value.ToString()) ?? Loads.First();
            Loads.Remove(load);
            _processedDropActionIds.Add(dropAction.ActionId);
            logger?.LogInformation("UpdateLoad: removed load {LoadId} for finished drop action {ActionId} at node {LastNodeId} (loads now {LoadCount})",
                load.Id, dropAction.ActionId, LastNodeId, Loads.Count);
        }
    }

    /// <summary>
    /// Finish the order.
    /// </summary>
    public void FinishOrder(ILogger? logger = null)
    {
        if (CurrentOrder == null || CurrentOrder.StartTime == null)
        {
            return;
        }
        var now = _timeProvider.GetUtcNow().DateTime;
        UpdateLoad(logger);
        string orderId = CurrentOrder.OrderId;
        CurrentOrder.TryFinish(now);
        if (CurrentOrder.StartTime == null)
        {
            logger?.LogInformation("FinishOrder: clearing CurrentOrder {OrderId} (all actions completed)", orderId);
            CurrentOrder = null;
            LastNodeSequenceId = 0;
        }
    }

    /// <summary>
    /// Abort the current order.
    /// </summary>
    /// <param name="actionId"></param>
    public void AbortOrder(string actionId)
    {
        if (CurrentOrder == null || CurrentOrder.StartTime == null)
        {
            return;
        }
        if (Charging)
        {
            Charging = false;
        }
        CurrentOrder!.Cancel(actionId);
        FinishOrder();
    }

    private static readonly Random _random = Random.Shared;

    /// <summary>
    /// Randomly generate or release errors.
    /// </summary>
    public void GenerateError()
    {
        bool act = _random.Next(2) == 1;
        if (Error != null && act)
        {
            Error = null;
            return;
        }
        act = act && _timeProvider.GetUtcNow().Minute % 5 == 0;
        Error = act ? ErrorGenerator.CreateError(CorrelationId) : null;
    }

    /// <summary>
    /// Initialize position from nodes.
    /// </summary>
    /// <param name="nodes">The nodes to select the position from.</param>
    public void InitializePosition(ICollection<GraphNode> nodes)
    {
        if (LastNodeId != null)
        {
            return;
        }
        if (nodes.Count == 0)
        {
            return;
        }
        int nodeIdx = _random.Next(nodes.Count - 1);
        var node = nodes.Skip(nodeIdx).Take(1).First();
        LastNodeId = node.Name;
        CurrentPosition = new AgvPosition { X = node.X, Y = node.Y, Theta = node.Theta, Z = 0, PositionInitialized = true };
        SimulatedBatteryCharge = _initialBatteryCharge;
    }
}

/// <summary>
/// A load.
/// </summary>
/// <param name="Id">The load id.</param>
/// <param name="PickStartTime">The time the pick action started.</param>
public record Load(string Id, DateTime PickStartTime);