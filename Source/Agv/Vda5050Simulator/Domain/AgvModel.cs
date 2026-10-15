namespace Gebhardt.Acc.Vda5050Simulator.Domain;

public enum OrderDecision
{
    Accepted,
    Updated,
    Duplicate,
    Rejected
}

public sealed record OrderResult(OrderDecision Decision, AgvError? Error = null);

/// <summary>
/// Mutable state and behavior of one simulated AGV. Pure domain code without I/O, mailbox or logging;
/// not thread-safe, the owning actor guarantees sequential access.
/// </summary>
public sealed class AgvModel(AgvId id, double maxSpeed, TimeSpan actionDuration)
{
    private const int MaxErrors = 20;
    private const int NurbsSamples = 50;

    private readonly List<ActionState> _actionStates = [];
    private readonly List<NodeState> _nodeStates = [];
    private readonly List<EdgeState> _edgeStates = [];
    private readonly List<AgvError> _errors = [];

    private readonly SortedDictionary<int, OrderNode> _nodeDefs = [];
    private readonly SortedDictionary<int, OrderEdge> _edgeDefs = [];
    private readonly Dictionary<string, AgvAction> _actionDefs = [];
    private readonly Dictionary<string, TimeSpan> _running = [];

    private double _x;
    private double _y;
    private double _theta;
    private string _mapId = string.Empty;
    private bool _positionInitialized;

    private int _activeEdgeSequenceId = -1;
    private List<(double X, double Y)> _geometry = [];
    private double[] _cumulativeLength = [];
    private double _progress;
    private double _edgeSpeed;

    public AgvId Id { get; } = id;

    public string OrderId { get; private set; } = string.Empty;

    public int OrderUpdateId { get; private set; }

    public string LastNodeId { get; private set; } = string.Empty;

    public int LastNodeSequenceId { get; private set; }

    public bool Driving { get; private set; }

    public double BatteryCharge { get; private set; } = 100;

    public bool Charging { get; private set; }

    /// <summary>
    /// True while the AGV still has nodes or edges to traverse or actions that are not finished or failed.
    /// </summary>
    public bool HasActiveOrder =>
        _nodeStates.Count > 0
        || _edgeStates.Count > 0
        || _actionStates.Any(a => a.Status is not (ActionStatus.Finished or ActionStatus.Failed));

    /// <summary>
    /// Validates an order against the current state (VDA5050 order acceptance rules) and applies it if valid.
    /// A rejected order adds an error to the state.
    /// </summary>
    public OrderResult TryAcceptOrder(Order order)
    {
        bool sameOrderId = OrderId.Length > 0 && order.OrderId == OrderId;

        if (sameOrderId)
        {
            if (order.OrderUpdateId < OrderUpdateId)
            {
                return Reject("orderUpdateError", order,
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
            return Reject("orderError", order,
                $"order {OrderId} is still active; a new order requires a finished order or a cancelOrder");
        }

        return TryStartOrder(order);
    }

    public void AddValidationError(string messageKind, string? referenceId, IReadOnlyList<string> errors)
    {
        var references = new List<ErrorReference> { new("topic", messageKind) };
        if (!string.IsNullOrEmpty(referenceId))
        {
            references.Add(new ErrorReference("orderId", referenceId));
        }

        AddError(new AgvError("validationError", ErrorLevel.Warning, string.Join("; ", errors), references));
    }

    /// <summary>
    /// Advances the simulation. Returns true if the published state changed (driving flag, reached node, action status).
    /// Position changes alone do not count as change.
    /// </summary>
    public bool Advance(TimeSpan dt)
    {
        bool changed = AdvanceRunningActions(dt);

        if (!_nodeDefs.TryGetValue(LastNodeSequenceId, out var currentNode))
        {
            return SetDriving(false) || changed;
        }

        changed |= StartNodeActions(currentNode);

        bool blockedByNodeActions = currentNode.Actions.Any(a => a.BlockingType != BlockingType.None && StatusOf(a.ActionId) is ActionStatus.Waiting or ActionStatus.Running);

        var nextEdge = _edgeDefs.Values.FirstOrDefault(e => e.SequenceId > LastNodeSequenceId);
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
            Charging,
            _actionStates.ToList(),
            _nodeStates.ToList(),
            _edgeStates.ToList(),
            _errors.ToList(),
            _positionInitialized ? new AgvPositionSnapshot(_x, _y, _theta, _mapId, true) : null);
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
        _actionStates.Clear();
        _nodeDefs.Clear();
        _edgeDefs.Clear();
        _actionDefs.Clear();
        _running.Clear();
        _activeEdgeSequenceId = -1;

        AddDefinitions(order);
        _actionStates.AddRange(CreateActionStates(order));
        RebuildNodeAndEdgeStates();

        var p = first.Position;
        SetPosition(p.X, p.Y, p.Theta ?? _theta, p.MapId);
        return new OrderResult(OrderDecision.Accepted);
    }

    private OrderResult TryUpdateOrder(Order order)
    {
        var baseEnd = _nodeDefs.Values.LastOrDefault(n => n.Released);
        var first = order.Nodes[0];
        if (baseEnd == null || first.NodeId != baseEnd.NodeId || first.SequenceId != baseEnd.SequenceId)
        {
            return Reject("orderUpdateError", order,
                $"first node {first.NodeId} (sequenceId {first.SequenceId}) does not match the end of the current base "
                + $"({baseEnd?.NodeId} / {baseEnd?.SequenceId})");
        }

        var removedActionIds = _nodeDefs.Values.Where(n => n.SequenceId > baseEnd.SequenceId).SelectMany(n => n.Actions)
            .Concat(_edgeDefs.Values.Where(e => e.SequenceId > baseEnd.SequenceId).SelectMany(e => e.Actions))
            .Select(a => a.ActionId)
            .ToHashSet(StringComparer.Ordinal);
        _actionStates.RemoveAll(a => a.Status == ActionStatus.Waiting && removedActionIds.Contains(a.ActionId));
        foreach (var key in _nodeDefs.Keys.Where(k => k > baseEnd.SequenceId).ToList())
        {
            _nodeDefs.Remove(key);
        }
        foreach (var key in _edgeDefs.Keys.Where(k => k > baseEnd.SequenceId).ToList())
        {
            _edgeDefs.Remove(key);
        }

        OrderUpdateId = order.OrderUpdateId;
        AddDefinitions(order);
        foreach (var action in CreateActionStates(order))
        {
            if (_actionStates.All(a => a.ActionId != action.ActionId))
            {
                _actionStates.Add(action);
            }
        }
        RebuildNodeAndEdgeStates();
        return new OrderResult(OrderDecision.Updated);
    }

    private void AddDefinitions(Order order)
    {
        foreach (var node in order.Nodes)
        {
            _nodeDefs[node.SequenceId] = node;
            foreach (var action in node.Actions)
            {
                _actionDefs[action.ActionId] = action;
            }
        }
        foreach (var edge in order.Edges)
        {
            _edgeDefs[edge.SequenceId] = edge;
            foreach (var action in edge.Actions)
            {
                _actionDefs[action.ActionId] = action;
            }
        }
    }

    private void RebuildNodeAndEdgeStates()
    {
        _nodeStates.Clear();
        _nodeStates.AddRange(_nodeDefs.Values
            .Where(n => n.SequenceId > LastNodeSequenceId)
            .Select(n => new NodeState(n.NodeId, n.SequenceId, n.Released)));
        _edgeStates.Clear();
        _edgeStates.AddRange(_edgeDefs.Values
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
        foreach (var actionId in _running.Keys.ToList())
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
        bool hardRunning = _running.Keys.Any(id => _actionDefs.TryGetValue(id, out var d) && d.BlockingType == BlockingType.Hard);
        if (hardRunning)
        {
            return false;
        }
        return action.BlockingType != BlockingType.Hard || _running.Count == 0;
    }

    private void StartAction(string actionId)
    {
        _running[actionId] = actionDuration;
        SetActionStatus(actionId, ActionStatus.Running);
    }

    private bool StartEdge(OrderEdge edge)
    {
        _activeEdgeSequenceId = edge.SequenceId;
        _progress = 0;
        _edgeSpeed = Math.Max(0.01, Math.Min(maxSpeed, edge.MaxSpeed ?? double.MaxValue));
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
        var endNode = _nodeDefs.Values.First(n => n.SequenceId == edge.SequenceId + 1);

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
        var points = new List<(double X, double Y)>();
        var endNode = _nodeDefs.Values.First(n => n.SequenceId == edge.SequenceId + 1);

        if (edge.Trajectory is { } trajectory)
        {
            points.AddRange(SampleNurbs(trajectory));
        }
        else
        {
            if (_positionInitialized)
            {
                points.Add((_x, _y));
            }
            points.Add((endNode.Position.X, endNode.Position.Y));
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

    private static List<(double X, double Y)> SampleNurbs(NurbsTrajectory curve)
    {
        int degree = curve.Degree;
        int count = curve.ControlPoints.Count;
        double start = curve.KnotVector[degree];
        double end = curve.KnotVector[count];
        var result = new List<(double X, double Y)>(NurbsSamples + 1);

        for (int s = 0; s <= NurbsSamples; s++)
        {
            double u = start + (end - start) * s / NurbsSamples;
            if (s == NurbsSamples)
            {
                var last = curve.ControlPoints[^1];
                result.Add((last.X, last.Y));
                continue;
            }

            double x = 0;
            double y = 0;
            double weightSum = 0;
            for (int i = 0; i < count; i++)
            {
                double basis = Basis(curve.KnotVector, i, degree, u) * curve.ControlPoints[i].Weight;
                x += basis * curve.ControlPoints[i].X;
                y += basis * curve.ControlPoints[i].Y;
                weightSum += basis;
            }
            result.Add(weightSum > 0 ? (x / weightSum, y / weightSum) : (curve.ControlPoints[0].X, curve.ControlPoints[0].Y));
        }
        return result;
    }

    private static double Basis(IReadOnlyList<double> knots, int i, int degree, double u)
    {
        if (degree == 0)
        {
            return knots[i] <= u && u < knots[i + 1] ? 1 : 0;
        }

        double result = 0;
        double leftSpan = knots[i + degree] - knots[i];
        if (leftSpan > 0)
        {
            result += (u - knots[i]) / leftSpan * Basis(knots, i, degree - 1, u);
        }
        double rightSpan = knots[i + degree + 1] - knots[i + 1];
        if (rightSpan > 0)
        {
            result += (knots[i + degree + 1] - u) / rightSpan * Basis(knots, i + 1, degree - 1, u);
        }
        return result;
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
        foreach (var state in _actionStates)
        {
            if (state.ActionId == actionId)
            {
                return state.Status;
            }
        }
        return null;
    }

    private void SetActionStatus(string actionId, ActionStatus status)
    {
        int index = _actionStates.FindIndex(a => a.ActionId == actionId);
        if (index >= 0)
        {
            _actionStates[index] = _actionStates[index] with { Status = status };
        }
    }

    private OrderResult Reject(string errorType, Order order, string description)
    {
        var error = new AgvError(errorType, ErrorLevel.Warning, description,
        [
            new ErrorReference("orderId", order.OrderId),
            new ErrorReference("orderUpdateId", order.OrderUpdateId.ToString())
        ]);
        AddError(error);
        return new OrderResult(OrderDecision.Rejected, error);
    }

    private void AddError(AgvError error)
    {
        _errors.Add(error);
        if (_errors.Count > MaxErrors)
        {
            _errors.RemoveAt(0);
        }
    }
}
