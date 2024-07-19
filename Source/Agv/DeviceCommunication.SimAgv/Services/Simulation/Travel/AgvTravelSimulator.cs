using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using Gebhardt.Acc.Common.Time;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Travel;

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

    private List<string> AddedVirtualNodes { get; set; } = [];

    private List<Guid> AddedVirtualEdges { get; set; } = [];

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
            bool agvGoesBackwards = sourceNode.IsStation();
            BaseEdge edge = graphEdge.Variant switch
            {
                EdgeType.Direct => new DirectEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards),
                EdgeType.Polypath => new PolypathEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards),
                EdgeType.Slanted => new SlantedEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards),
                EdgeType.Bezier1 => new BezierEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards),
                EdgeType.Bezier2 => new BezierEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards),
                _ => new DirectEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards)
            };
            Edges.Add(edge);
        }

        _nodes = graph.Nodes.ToList();
    }

    /// <inheritdoc />
    public void EstimateTravelTimes(IReadOnlyList<string> nodes, IReadOnlyList<Guid> edges, DateTime entryTime, double initialVelocity)
    {
        Reset();
        if (nodes.Count < 1)
        {
            return;
        }

        double entryAngle = 0;
        BaseEdge? followingEdge;

        if (nodes.Count == 1 && CurrentNode != null && !AddedVirtualNodes.Contains(CurrentNode.Id))
        {
            EstimateTimeForChargeAction(nodes);
            return;
        }

        if (nodes.Count == 1 && CurrentNode != null && AddedVirtualNodes.Contains(CurrentNode.Id))
        {
            EstimateTimeToGetBackToGraph(nodes.Single(), entryTime, initialVelocity);
            return;
        }

        foreach (var edge in edges.Select((id, index) => new { id, index }))
        {
            if (edge.index >= edges.Count - 1)
            {
                break;
            }

            followingEdge = Edges.Single(e => e.Id == edge.id);
            var nextEdge = Edges.First(e => e.Id == edges[edge.index + 1]);
            bool turnAgvAtExit = FollowingEdges.Count == 0 && followingEdge.SourceNode.IsStation();
            followingEdge.SetAngles(entryAngle, nextEdge.SourceNode, nextEdge.ReferenceTarget, turnAgvAtExit);
            followingEdge.EstimateTravelTime(_agvData, initialVelocity, entryTime);
            FollowingEdges.Add(followingEdge);

            entryTime = followingEdge.EstimatedExitTime;
            initialVelocity = turnAgvAtExit ? 0 : followingEdge.ExitVelocity;
            entryAngle = followingEdge.ExitAngle;
        }
        followingEdge = Edges.Single(e => e.Id == edges.Last());
        followingEdge.SetAngles(entryAngle, null, null);
        followingEdge.EstimateTravelTime(_agvData, initialVelocity, entryTime);
        FollowingEdges.Add(followingEdge);

        CurrentEdge = FollowingEdges.FirstOrDefault();
        CurrentNode = CurrentEdge?.SourceNode;

        if (FollowingEdges.Any())
        {
            TravelDuration = FollowingEdges.Last().EstimatedExitTime - CurrentEdge!.EntryTime;
        }
    }

    private void EstimateTimeForChargeAction(IReadOnlyList<string> nodes)
    {
        CurrentEdge = null;
        CurrentNode = _nodes.Single(e => e.Id == nodes.Single());

        // already there
        TravelDuration = TimeSpan.Zero;
    }

    private void EstimateTimeToGetBackToGraph(string targetNode, DateTime entryTime, double initialVelocity)
    {
        ResetAddedVirtualNodes(CurrentNode!.Id);
        var target = _nodes.Single(n => n.Id == targetNode);
        var graphEdge = GraphEdge.CreateDirect(CurrentNode.Id, targetNode, (float)Mathematics.EuclideanDistance(CurrentNode, target), Guid.NewGuid());
        var edge = new DirectEdge(graphEdge, CurrentNode, target);
        Edges.Add(edge);
        AddedVirtualEdges.Add(edge.Id);
        var followingEdge = edge;
        followingEdge.SetAngles(0, null, null);
        followingEdge.EstimateTravelTime(_agvData, initialVelocity, entryTime);
        FollowingEdges.Add(followingEdge);
        CurrentEdge = FollowingEdges.FirstOrDefault();
        CurrentNode = CurrentEdge?.SourceNode;
        if (FollowingEdges.Any())
        {
            TravelDuration = FollowingEdges.Last().EstimatedExitTime - CurrentEdge!.EntryTime;
        }
    }

    /// <inheritdoc />
    public void EstimateTravelTimes(AgvPosition targetPosition, DateTime entryTime, double initialVelocity)
    {
        var virtualSource = new GraphNode(Guid.NewGuid().ToString()) { X = Position.X, Y = Position.Y };
        var virtualTarget = new GraphNode(Guid.NewGuid().ToString()) { X = targetPosition.X, Y = targetPosition.Y };
        _nodes.Add(virtualSource);
        AddedVirtualNodes.Add(virtualSource.Id);
        _nodes.Add(virtualTarget);
        AddedVirtualNodes.Add(virtualTarget.Id);
        
        var graphEdge = GraphEdge.CreateDirect(virtualSource.Id, virtualTarget.Id, (float)Mathematics.EuclideanDistance(virtualSource, virtualTarget), Guid.NewGuid());
        var edge = new DirectEdge(graphEdge, virtualSource, virtualTarget);
        Edges.Add(edge);
        AddedVirtualEdges.Add(edge.Id);
        
        var followingEdge = edge;
        followingEdge.SetAngles(0, null, null);
        followingEdge.EstimateTravelTime(_agvData, initialVelocity, entryTime);
        FollowingEdges.Add(followingEdge);
        
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
            CurrentNode = CurrentEdge.TargetNode;
            Position = new AgvPosition(true, CurrentNode.X, CurrentNode.Y, CurrentNode.Z, Position.Theta);
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

    private void ResetAddedVirtualNodes(string nodeToKeep = "")
    {
        foreach (string node in AddedVirtualNodes.Where(n => n!=nodeToKeep))
        {
            _nodes.Remove(_nodes.Single(n => n.Id == node));
        }
        AddedVirtualNodes = AddedVirtualNodes.Except(AddedVirtualNodes.Where(n => n!=nodeToKeep)).ToList();
    }

    private void Reset()
    {
        FollowingEdges = new List<BaseEdge>();
        CurrentEdge = null;
        TravelDuration = null;
        _pauseTime = TimeSpan.Zero;
        _pausedFrom = null;
        foreach (var addedEdge in AddedVirtualEdges)
        {
            Edges.Remove(Edges.Single(e => e.Id == addedEdge));
        }
        AddedVirtualEdges = [];
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