using Gebhardt.Acc.Common.Domain.Models;
using static Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Constants.Vda5050Specifications;

namespace DeviceCommunication.Vda5050.Simulator.Domain;

public enum OrderDecision
{
    Accepted,
    Updated,
    Duplicate,
    Rejected
}

public sealed record OrderResult(OrderDecision Decision, Error? Error = null);

/// <summary>
/// Mutable state and behavior of one simulated AGV. Pure domain code without I/O, mailbox or logging;
/// not thread-safe, the owning actor guarantees sequential access.
/// </summary>
public sealed class AgvModel
{
    private const int MaxErrors = 20;
    private readonly OrderedDictionary<string, ActionState> _actionStates = new(StringComparer.Ordinal);
    private readonly List<NodeState> _nodeStates = [];
    private readonly List<EdgeState> _edgeStates = [];
    private readonly List<Error> _errors = [];

    private readonly SortedDictionary<int, OrderNode> _orderNodes = [];
    private readonly SortedDictionary<int, OrderEdge> _orderEdges = [];
    private readonly Dictionary<string, AgvAction> _orderActions = [];
    private readonly Dictionary<string, TimeSpan> _running = [];
    private readonly List<Load> _loads = [new("LoadId", "LoadType", "LoadPosition")];

    private double _x;
    private double _y;
    private double _theta;
    private string _mapId = "mapId (to be derivded from LIF)";
    private bool _positionInitialized = true;

    private int _activeEdgeSequenceId = -1;
    private List<PolygonPoint> _geometry = [];
    private double[] _cumulativeLength = [];
    private double _progress;
    private double _edgeSpeed;
    private double _batteryChargeRate;
    private double _batteryDischargeRate;
    private double _maxSpeed;

    private AgvOperatingMode _operatingMode = AgvOperatingMode.Automatic;
    
    private readonly TimeSpan _actionDuration;

    public AgvModel(AgvConfiguration configuration, TimeSpan actionDuration)
    {
        ApplyConfiguration(configuration); 
        _actionDuration = actionDuration;
        AgvIdentity = configuration;
    }
    
    public AgvIdentity AgvIdentity { get; }
    
    public string OrderId { get; private set; } = string.Empty;

    public int OrderUpdateId { get; private set; }

    public string LastNodeId { get; private set; } = string.Empty;

    public int LastNodeSequenceId { get; private set; }

    public bool Driving { get; private set; }

    public double BatteryCharge { get; private set; } = 100;

    public bool IsCharging { get; } = false;
    
    /// <summary>
    /// True while the AGV still has nodes or edges to traverse or actions that are not finished or failed.
    /// </summary>
    private bool HasActiveOrder =>
        _nodeStates.Count > 0
        || _edgeStates.Count > 0
        || _actionStates.Values.Any(a => _orderActions.ContainsKey(a.ActionId) && a.Status is not (ActionStatus.Finished or ActionStatus.Failed));
    
    public void ApplyConfiguration(AgvConfiguration configuration)
    {
        _batteryChargeRate = configuration.BatteryChargeRate;
        _batteryDischargeRate = configuration.BatteryDischargeRate;
        _maxSpeed = configuration.MaxSpeed;
    }

    /// <summary>
    /// Adds or updates the state of an instant action in the actionStates of the VDA5050 state.
    /// </summary>
    public void SetInstantActionState(string actionId, string actionType, ActionStatus status)
    {
        if (_actionStates.TryGetValue(actionId, out var existing))
        {
            _actionStates[actionId] = existing with { Status = status };
        }
        else
        {
            _actionStates.Add(actionId, new ActionState(actionId, actionType, status));
        }
    }

    /// <summary>
    /// Validates an order against the current state (VDA5050 order acceptance rules) and applies it if valid.
    /// A rejected order adds an error to the state.
    /// </summary>
    public OrderResult TryAcceptOrder(Order order)
    {
        if (_operatingMode != AgvOperatingMode.Automatic)
        {
            return Reject(Errors.MobileRobotNotAutomatic, order, "Receival of an order while not in 'AUTOMATIC'");
        }

        bool sameOrderId = OrderId.Length > 0 && order.OrderId == OrderId;

        if (sameOrderId)
        {
            if (order.OrderUpdateId < OrderUpdateId)
            {
                return Reject(ErrorTypes.OrderUpdateError, order,
                    $"orderUpdateId {order.OrderUpdateId} is lower than the current orderUpdateId {OrderUpdateId}");
            }
            if (order.OrderUpdateId == OrderUpdateId)
            {
                return new OrderResult(OrderDecision.Duplicate);
            }
            if (HasActiveOrder)
            {
                return TryUpdateOrder(order);
            }
        }
        else if (HasActiveOrder)
        {
            return Reject(ErrorTypes.OrderError, order,
                $"order {OrderId} is still active; a new order requires a finished order or a cancelOrder");
        }

        return TryStartOrder(order);
    }

    /// <summary>
    /// Switches the operating mode. Leaving automatic mode aborts the current order.
    /// </summary>
    public void SwitchOperatingMode(AgvOperatingMode operatingMode)
    {
        _operatingMode = operatingMode;
        if (operatingMode == AgvOperatingMode.Automatic)
        {
            _errors.RemoveAll(e => e.ErrorType == Errors.MobileRobotNotAutomatic);
            return;
        }

        foreach (string actionId in _running.Keys.ToList())
        {
            SetActionStatus(actionId, ActionStatus.Failed);
        }
        _running.Clear();
        _actionStates.Clear();
        _nodeStates.Clear();
        _edgeStates.Clear();
        _orderNodes.Clear();
        _orderEdges.Clear();
        _orderActions.Clear();
        _activeEdgeSequenceId = -1;
        _geometry = [];
        _cumulativeLength = [];
        OrderId = string.Empty;
        OrderUpdateId = 0;
        LastNodeId = string.Empty;
        LastNodeSequenceId = 0;
        SetDriving(false);
    }

    /// <summary>
    /// Sets the AGV to the given position. Only allowed outside of automatic mode; otherwise an error is added to the state.
    /// </summary>
    public CommandResult SetPositionManually(double x, double y, double theta, string mapId)
    {
        if (_operatingMode != AgvOperatingMode.Manual)
        {
            AddNotManualError();    
            return CommandResult.Fail("Agv not in manual mode and can not be moved.");
        }

        SetPosition(x, y, theta, mapId);
        LastNodeId = string.Empty;
        LastNodeSequenceId = 0;
        return CommandResult.Ok;
    }

    /// <summary>
    /// Adds a load to the AGV. Fails if the load position is already occupied.
    /// </summary>
    public CommandResult AddLoad(string loadId, string? loadType, string? loadPosition)
    {
        if (_loads.Any(l => l.LoadPosition == loadPosition))
        {
            return CommandResult.Fail($"Load position '{loadPosition}' is already occupied.");
        }

        _loads.Add(new Load(loadId, loadType, loadPosition));
        return CommandResult.Ok;
    }

    /// <summary>
    /// Removes the load with the given id from the AGV. Fails if the load does not exist.
    /// </summary>
    public CommandResult RemoveLoad(string loadId)
    {
        return _loads.RemoveAll(l => l.LoadId == loadId) > 0
            ? CommandResult.Ok
            : CommandResult.Fail($"Load '{loadId}' does not exist.");
    }

    private void AddNotManualError()
    {
        _errors.RemoveAll(e => e.ErrorType == Errors.MobileRobotNotManual);
        AddError(new Error(Errors.MobileRobotNotManual, ErrorLevel.Warning, "Manual update of state while not in 'MANUAL'", null));
    }

    public void AddValidationError(string topic, string? id, params string[] errors)
    {
        var references = new List<ErrorReference> { new("topic", topic) };
        if (!string.IsNullOrEmpty(id))
        {
            references.Add(new ErrorReference("Id", id));
        }

        AddError(new Error("validationError", ErrorLevel.Warning, string.Join("; ", errors), references));
    }

    public void AddValidationError(string topic, string? id, IReadOnlyList<string> errors)
    {
        var references = new List<ErrorReference> { new("topic", topic) };
        if (!string.IsNullOrEmpty(id))
        {
            references.Add(new ErrorReference("Id", id));
        }

        AddError(new Error("validationError", ErrorLevel.Warning, string.Join("; ", errors), references));
    }
    
    /// <summary>
    /// Advances the simulation. Returns true if the published state changed (driving flag, reached node, action status).
    /// Position changes alone do not count as change.
    /// </summary>
    public bool Advance(TimeSpan dt)
    {
        bool changed = AdvanceRunningActions(dt);

        if (!_orderNodes.TryGetValue(LastNodeSequenceId, out var currentNode))
        {
            return SetDriving(false) || changed;
        }

        changed |= StartNodeActions(currentNode);

        bool blockedByNodeActions = currentNode.Actions.Any(a => a.BlockingType != BlockingType.None && StatusOf(a.ActionId) is ActionStatus.Waiting or ActionStatus.Running);

        var nextEdge = _orderEdges.Values.FirstOrDefault(e => e.SequenceId > LastNodeSequenceId);
        if (blockedByNodeActions || nextEdge is not { Released: true })
        {
            return SetDriving(false) || changed;
        }

        if (_activeEdgeSequenceId != nextEdge.SequenceId)
        {
            changed |= StartEdge(nextEdge);
        }

        changed |= SetDriving(true);
        _progress += _edgeSpeed * dt.TotalSeconds;
        double length = _cumulativeLength.Length > 0 ? _cumulativeLength[^1] : 0;
        if (_progress >= length)
        {
            changed |= ArriveAtEndNode(nextEdge);
        }
        else
        {
            UpdatePosition();
        }
        return changed;
    }

    public AgvStateSnapshot CreateSnapshot(long headerId, DateTimeOffset timestamp)
    {
        return new AgvStateSnapshot(
            headerId,
            timestamp,
            OrderId,
            OrderUpdateId,
            LastNodeId,
            LastNodeSequenceId,
            Driving,
            BatteryCharge,
            IsCharging,
            _actionStates.Values.ToList(),
            _nodeStates.ToList(),
            _edgeStates.ToList(),
            _errors.ToList(),
            new AgvPosition(_x, _y, _theta, _mapId, _positionInitialized),
            _operatingMode,
            _loads.ToList(),
            new Velocity(Driving && _activeEdgeSequenceId >= 0 ? _edgeSpeed : 0, 0, 0));
    }

    private OrderResult TryStartOrder(Order order)
    {
        var first = order.Nodes[0];
        if (LastNodeId.Length > 0 && first.NodeId != LastNodeId)
        {
            return Reject("orderError", order,
                $"first node {first.NodeId} is not the current node {LastNodeId} of the AGV");
        }

        OrderId = order.OrderId;
        OrderUpdateId = order.OrderUpdateId;
        LastNodeId = first.NodeId;
        LastNodeSequenceId = first.SequenceId;
        _errors.Clear();
        foreach (string key in _actionStates.Keys.Where(k => _orderActions.ContainsKey(k)).ToList())
        {
            _actionStates.Remove(key);
        }
        _orderNodes.Clear();
        _orderEdges.Clear();
        _orderActions.Clear();
        _running.Clear();
        _activeEdgeSequenceId = -1;

        AddOrderData(order);
        foreach (var action in CreateActionStates(order))
        {
            _actionStates[action.ActionId] = action;
        }
        RebuildNodeAndEdgeStates();

        var p = first.Position;
        SetPosition(p.X, p.Y, p.Theta ?? _theta, p.MapId);
        return new OrderResult(OrderDecision.Accepted);
    }

    private OrderResult TryUpdateOrder(Order order)
    {
        var baseEnd = _orderNodes.Values.LastOrDefault(n => n.Released);
        var first = order.Nodes[0];
        if (baseEnd == null || first.NodeId != baseEnd.NodeId || first.SequenceId != baseEnd.SequenceId)
        {
            return Reject("orderUpdateError", order,
                $"first node {first.NodeId} (sequenceId {first.SequenceId}) does not match the end of the current base "
                + $"({baseEnd?.NodeId} / {baseEnd?.SequenceId})");
        }

        var removedActionIds = _orderNodes.Values.Where(n => n.SequenceId > baseEnd.SequenceId).SelectMany(n => n.Actions)
            .Concat(_orderEdges.Values.Where(e => e.SequenceId > baseEnd.SequenceId).SelectMany(e => e.Actions))
            .Select(a => a.ActionId)
            .ToHashSet(StringComparer.Ordinal);
        foreach (string actionId in removedActionIds)
        {
            if (_actionStates.TryGetValue(actionId, out var removed) && removed.Status == ActionStatus.Waiting)
            {
                _actionStates.Remove(actionId);
            }
        }
        
        foreach (int key in _orderNodes.Keys.Where(k => k > baseEnd.SequenceId).ToList())
        {
            _orderNodes.Remove(key);
        }

        foreach (int key in _orderEdges.Keys.Where(k => k > baseEnd.SequenceId).ToList())
        {
            _orderEdges.Remove(key);
        }

        OrderUpdateId = order.OrderUpdateId;
        AddOrderData(order);
        foreach (var action in CreateActionStates(order))
        {
            _actionStates.TryAdd(action.ActionId, action);
        }
        RebuildNodeAndEdgeStates();
        return new OrderResult(OrderDecision.Updated);
    }

    private void AddOrderData(Order order)
    {
        foreach (var node in order.Nodes)
        {
            _orderNodes[node.SequenceId] = node;
            foreach (var action in node.Actions)
            {
                _orderActions[action.ActionId] = action;
            }
        }
        foreach (var edge in order.Edges)
        {
            _orderEdges[edge.SequenceId] = edge;
            foreach (var action in edge.Actions)
            {
                _orderActions[action.ActionId] = action;
            }
        }
    }

    private void RebuildNodeAndEdgeStates()
    {
        _nodeStates.Clear();
        _nodeStates.AddRange(_orderNodes.Values
            .Where(n => n.SequenceId > LastNodeSequenceId)
            .Select(n => new NodeState(n.NodeId, n.SequenceId, n.Released)));
        _edgeStates.Clear();
        _edgeStates.AddRange(_orderEdges.Values
            .Where(e => e.SequenceId > LastNodeSequenceId)
            .Select(e => new EdgeState(e.EdgeId, e.SequenceId, e.Released)));
    }

    private static IEnumerable<ActionState> CreateActionStates(Order order)
    {
        return order.Nodes.SelectMany(n => n.Actions)
            .Concat(order.Edges.SelectMany(e => e.Actions))
            .Select(a => new ActionState(a.ActionId, a.ActionType, ActionStatus.Waiting));
    }

    private bool AdvanceRunningActions(TimeSpan dt)
    {
        bool changed = false;
        foreach (string actionId in _running.Keys.ToList())
        {
            var remaining = _running[actionId] - dt;
            if (remaining <= TimeSpan.Zero)
            {
                _running.Remove(actionId);
                SetActionStatus(actionId, ActionStatus.Finished);
                changed = true;
            }
            else
            {
                _running[actionId] = remaining;
            }
        }
        return changed;
    }

    private bool StartNodeActions(OrderNode node)
    {
        bool changed = false;
        foreach (var action in node.Actions)
        {
            if (StatusOf(action.ActionId) != ActionStatus.Waiting)
            {
                continue;
            }
            if (!CanStart(action))
            {
                break;
            }
            StartAction(action.ActionId);
            changed = true;
        }
        return changed;
    }

    private bool CanStart(AgvAction action)
    {
        bool hardRunning = _running.Keys.Any(id => _orderActions.TryGetValue(id, out var d) && d.BlockingType == BlockingType.Hard);
        if (hardRunning)
        {
            return false;
        }
        return action.BlockingType != BlockingType.Hard || _running.Count == 0;
    }

    private void StartAction(string actionId)
    {
        _running[actionId] = _actionDuration;
        SetActionStatus(actionId, ActionStatus.Running);
    }

    private bool StartEdge(OrderEdge edge)
    {
        _activeEdgeSequenceId = edge.SequenceId;
        _progress = 0;
        _edgeSpeed = Math.Max(0.01, Math.Min(_maxSpeed, edge.MaxSpeed ?? double.MaxValue));
        BuildGeometry(edge);

        bool changed = false;
        foreach (var action in edge.Actions)
        {
            if (StatusOf(action.ActionId) == ActionStatus.Waiting)
            {
                StartAction(action.ActionId);
                changed = true;
            }
        }
        return changed;
    }

    private bool ArriveAtEndNode(OrderEdge edge)
    {
        var endNode = _orderNodes.Values.First(n => n.SequenceId == edge.SequenceId + 1);

        LastNodeId = endNode.NodeId;
        LastNodeSequenceId = endNode.SequenceId;
        _activeEdgeSequenceId = -1;
        _geometry = [];
        _cumulativeLength = [];
        _nodeStates.RemoveAll(n => n.SequenceId <= LastNodeSequenceId);
        _edgeStates.RemoveAll(e => e.SequenceId <= LastNodeSequenceId);

        var p = endNode.Position;
        SetPosition(p.X, p.Y, p.Theta ?? _theta, p.MapId);
        return true;
    }

    private void BuildGeometry(OrderEdge edge)
    {
        var points = new List<PolygonPoint>();
        var endNode = _orderNodes.Values.First(n => n.SequenceId == edge.SequenceId + 1);

        if (edge.Trajectory is { } trajectory)
        {
            points = trajectory.Sample().ToList();
        }
        else
        {
            if (_positionInitialized)
            {
                points.Add(new PolygonPoint(_x, _y));
            }
            points.Add(new PolygonPoint(endNode.Position.X, endNode.Position.Y));
        }

        _geometry = points;
        _cumulativeLength = new double[points.Count];
        for (int i = 1; i < points.Count; i++)
        {
            double dx = points[i].X - points[i - 1].X;
            double dy = points[i].Y - points[i - 1].Y;
            _cumulativeLength[i] = _cumulativeLength[i - 1] + Math.Sqrt(dx * dx + dy * dy);
        }
        if (points.Count > 0)
        {
            _mapId = endNode.Position.MapId;
        }
    }

    private void UpdatePosition()
    {
        if (_geometry.Count < 2)
        {
            return;
        }
        int i = 1;
        while (i < _cumulativeLength.Length - 1 && _cumulativeLength[i] < _progress)
        {
            i++;
        }
        double segmentLength = _cumulativeLength[i] - _cumulativeLength[i - 1];
        double t = segmentLength > 0 ? (_progress - _cumulativeLength[i - 1]) / segmentLength : 1;
        var a = _geometry[i - 1];
        var b = _geometry[i];
        _x = a.X + (b.X - a.X) * t;
        _y = a.Y + (b.Y - a.Y) * t;
        _theta = Math.Atan2(b.Y - a.Y, b.X - a.X);
        _positionInitialized = true;
    }

    public void UpdateBattery(TimeSpan dt)
    {
        double chargeDiff;
        double timeDiffS = dt.TotalSeconds;
        if (IsCharging)
        {
            chargeDiff = timeDiffS * _batteryChargeRate;
        }
        else
        {
            chargeDiff = timeDiffS * -_batteryDischargeRate;
        }

        BatteryCharge = Math.Clamp(BatteryCharge + chargeDiff, 0, 100);
    }

    private void SetPosition(double x, double y, double theta, string mapId)
    {
        _x = x;
        _y = y;
        _theta = theta;
        _mapId = mapId;
        _positionInitialized = true;
    }

    private bool SetDriving(bool driving)
    {
        if (Driving == driving)
        {
            return false;
        }
        Driving = driving;
        return true;
    }

    private ActionStatus? StatusOf(string actionId)
    {
        return _actionStates.TryGetValue(actionId, out var state) ? state.Status : null;
    }

    private void SetActionStatus(string actionId, ActionStatus status)
    {
        if (_actionStates.TryGetValue(actionId, out var state))
        {
            _actionStates[actionId] = state with { Status = status };
        }
    }

    private OrderResult Reject(string errorType, Order order, string description)
    {
        var error = new Error(errorType, ErrorLevel.Warning, description,
        [
            new ErrorReference("orderId", order.OrderId),
            new ErrorReference("orderUpdateId", order.OrderUpdateId.ToString())
        ]);
        AddError(error);
        return new OrderResult(OrderDecision.Rejected, error);
    }

    private void AddError(Error error)
    {
        _errors.Add(error);
        if (_errors.Count > MaxErrors)
        {
            _errors.RemoveAt(0);
        }
    }
}
