using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Time;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;

/// <inheritdoc />
public class AgvTravelSimulator : IAgvTravelSimulator
{
    private readonly List<GraphNode> _nodes = new();

    private DateTime? _pausedFrom;
    private TimeSpan _pauseTime = TimeSpan.Zero;

    private readonly AgvData _agvData;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// All edges of the agvs graph as <see cref="BaseEdge"/>.
    /// </summary>
    public List<BaseEdge> Edges { get; } = new();

    /// <summary>
    /// The edges the agv is about to travel on.
    /// </summary>
    public List<BaseEdge> FollowingEdges { get; private set; } = new();

    /// <inheritdoc />
    public BaseEdge? CurrentEdge { get; private set; }

    /// <inheritdoc />
    public GraphNode? CurrentNode { get; private set; }

    /// <inheritdoc />
    public TimeSpan? TravelDuration { get; private set; }

    /// <inheritdoc />
    public TimeSpan PauseTime
    {
        get
        {
            var pauseTime = _pauseTime;
            if (_pausedFrom != null)
            {
                var activePause = _timeProvider.Now() - _pausedFrom.Value;
                pauseTime += activePause;
            }
            return pauseTime;
        }
    }

    /// <inheritdoc />
    public AgvPosition Position { get; private set; }

    /// <summary>
    /// Agv orientation on previous edge.
    /// </summary>
    private float OrientationOnPreviousEdge { get; set; }

    /// <inheritdoc cref="IAgvTravelSimulator"/>>
    public AgvTravelSimulator(AgvGraph? graph, AgvData data, AgvPosition position, ITimeProvider timeProvider)
    {
        Position = position;
        _agvData = data;
        _timeProvider = timeProvider;
        if (graph == null)
        {
            return;
        }
        foreach (var graphEdge in graph.Edges)
        {
            var sourceNode = graph.Nodes.Single(n => n.Id == graphEdge.FromNodeId);
            var targetNode = graph.Nodes.Single(n => n.Id == graphEdge.ToNodeId);
            BaseEdge edge = graphEdge.Variant switch
            {
                EdgeType.Direct => new DirectEdge(graphEdge, sourceNode, targetNode),
                EdgeType.Polypath => new Polypath(graphEdge, sourceNode, targetNode),
                EdgeType.Slanted => new SlantedEdge(graphEdge, sourceNode, targetNode),
                EdgeType.Bezier1 => new BezierEdge(graphEdge, sourceNode, targetNode),
                EdgeType.Bezier2 => new BezierEdge(graphEdge, sourceNode, targetNode),
                _ => new DirectEdge(graphEdge, sourceNode, targetNode)
            };
            Edges.Add(edge);
        }

        _nodes = graph.Nodes.ToList();
    }

    /// <inheritdoc />
    public void EstimateTravelTimes(IReadOnlyList<string> nodes, DateTime entryTime, double initialVelocity)
    {
        Reset();
        if (nodes.Count < 1)
        {
            return;
        }

        double entryAngle = 0;
        BaseEdge? edge;

        // e.g. charge action
        if (nodes.Count == 1)
        {
            // CurrentEdge = FollowingEdges.FirstOrDefault();
            CurrentEdge = null;
            CurrentNode = _nodes.Single(e => e.Id == nodes.Single());

            // already there
            TravelDuration = TimeSpan.Zero;
            return;
        }

        foreach (var node in nodes.Select((id, index) => new {id, index}))
        {
            if (node.index >= nodes.Count - 2)
            {
                break;
            }
            if (node.id == nodes[node.index + 1]) // node duplication
            {
                continue;
            }

            edge = Edges.Single(e => e.FromNodeId == node.id && e.ToNodeId == nodes[node.index + 1]);
            var nextEdge = Edges.First(e => e.FromNodeId == nodes[node.index + 1] && e.ToNodeId == nodes[node.index + 2]);
            edge.SetAngles(entryAngle, nextEdge.SourceNode, nextEdge.ReferenceTarget);
            edge.EstimateTravelTime(_agvData, initialVelocity, entryTime);
            entryTime = edge.EstimatedExitTime;
            initialVelocity = edge.ExitVelocity;
            entryAngle = edge.ExitAngle;
            FollowingEdges.Add(edge);
        }
        edge = Edges.Single(e => e.FromNodeId == nodes[^2] && e.ToNodeId == nodes[^1]);
        edge.SetAngles(entryAngle, null, null);
        edge.EstimateTravelTime(_agvData, initialVelocity, entryTime);
        FollowingEdges.Add(edge);

        CurrentEdge = FollowingEdges.FirstOrDefault();
        CurrentNode = CurrentEdge?.SourceNode;

        if (FollowingEdges.Any())
        {
            TravelDuration = FollowingEdges.Last().EstimatedExitTime - CurrentEdge!.EntryTime;
        }
    }

    /// <inheritdoc />
    public void UpdatePosition()
    {
        var currentTime = _timeProvider.Now();
        // deduct pause time from currentTime, so simulate the time as if the agv was not paused
        currentTime -= PauseTime;

        if (!FollowingEdges.Any())
        {
            return;
        }

        // limit currentTime to the estimated exit time of the last edge, as the agv does not move further.
        var estimatedExitTime = FollowingEdges.Last().EstimatedExitTime;
        currentTime = currentTime > estimatedExitTime ? estimatedExitTime : currentTime;
        CurrentEdge = GetCurrentEdge(currentTime);
        var entryTime = FollowingEdges.First().EntryTime;
        double? progress = (currentTime - entryTime) / TravelDuration;
        CurrentNode = CurrentEdge.SourceNode;
        Position = CurrentEdge.GetCurrentPosition(currentTime, _agvData, OrientationOnPreviousEdge);

        // Agv is at the end of the route
        if (Math.Abs(progress!.Value - 1d) < double.Epsilon)
        {
            float theta = Position.Theta;
            // quick turn at station, as our Agv are not yet capable of reversing  
            if (CurrentEdge.TargetNode.IsStation())
            {
                theta -= (float) Math.PI;
            }
            CurrentNode = CurrentEdge.TargetNode;
            Position = new AgvPosition(true, CurrentNode.X, CurrentNode.Y, CurrentNode.Z, theta);
        }
    }

    /// <inheritdoc />
    public void Pause()
    {
        _pausedFrom = _timeProvider.Now();
    }

    /// <inheritdoc />
    public void Resume()
    {
        if (_pausedFrom == null)
        {
            // not paused at all
            return;
        }

        _pauseTime += _timeProvider.Now() - _pausedFrom.Value;
        _pausedFrom = null;
    }

    /// <inheritdoc />
    public void FinishTravel()
    {
        Reset();
    }

    private void Reset()
    {
        FollowingEdges = new List<BaseEdge>();
        CurrentEdge = null;
        TravelDuration = null;
        _pauseTime = TimeSpan.Zero;
        _pausedFrom = null;
    }

    private BaseEdge GetCurrentEdge(DateTime currentTime)
    {
        var current = FollowingEdges.SingleOrDefault(e => e.EstimatedExitTime > currentTime && e.EntryTime <= currentTime);
        if ((current != null && !current.Equals(CurrentEdge)) || (current == null && CurrentEdge != null))
        {
            OrientationOnPreviousEdge = Position.Theta;
        }
        return current ?? FollowingEdges.Last();
    }

    /// <inheritdoc />
    public BaseEdge GetShortestEdge()
    {
        return FollowingEdges.MinBy(e => e.EstimatedExitTime - e.InitialTurnFinishTime) ?? CurrentEdge!;
    }
}