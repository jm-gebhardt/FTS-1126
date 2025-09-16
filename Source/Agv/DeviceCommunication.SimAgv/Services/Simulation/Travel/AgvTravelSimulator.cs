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

    private readonly ITimeProvider _timeProvider;
    private readonly DateTime _timeStamp = DateTime.UtcNow;

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
    public float CurrentEdgeDistance { get; private set; }

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
    public AgvTravelSimulator(AgvGraph? graph, AgvPosition position, ITimeProvider timeProvider)
    {
        Position = position;
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
                EdgeType.Direct => new DirectEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards) { TimeStamp = _timeStamp },
                EdgeType.Polypath => new PolypathEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards) { TimeStamp = _timeStamp },
                EdgeType.Slanted => new SlantedEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards) { TimeStamp = _timeStamp },
                EdgeType.Bezier2 => new BezierEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards) { TimeStamp = _timeStamp },
                EdgeType.Bezier3 => new BezierEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards) { TimeStamp = _timeStamp },
                EdgeType.Bezier5 => new BezierEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards) { TimeStamp = _timeStamp },
                _ => new DirectEdge(graphEdge, sourceNode, targetNode, agvGoesBackwards) { TimeStamp = _timeStamp }
            };
            Edges.Add(edge);
        }

        _nodes = graph.Nodes.ToList();
    }

    /// <inheritdoc />
    public void EstimateTravelTimes(IReadOnlyList<string> nodes, IReadOnlyList<Guid> edges, DateTime entryTime, AgvData agvData, double initialVelocity)
    {
        Reset();
        if (nodes.Count < 1)
        {
            return;
        }

        double entryAngle = 0;
        BaseEdge? followingEdge;

        if (nodes.Count == 1 && CurrentNode != null && !AddedVirtualNodes.Contains(CurrentNode.Name))
        {
            EstimateTimeForChargeAction(nodes);
            return;
        }

        if (nodes.Count == 1 && CurrentNode != null && AddedVirtualNodes.Contains(CurrentNode.Name))
        {
            EstimateTimeToGetBackToGraph(nodes.Single(), entryTime, agvData, initialVelocity);
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
            followingEdge.EstimateTravelTime(agvData, initialVelocity, entryTime);
            FollowingEdges.Add(followingEdge);

            entryTime = followingEdge.EstimatedExitTime;
            initialVelocity = turnAgvAtExit ? 0 : followingEdge.ExitVelocity;
            entryAngle = followingEdge.ExitAngle;
        }
        followingEdge = Edges.Single(e => e.Id == edges.Last());
        followingEdge.SetAngles(entryAngle, null, null);
        followingEdge.EstimateTravelTime(agvData, initialVelocity, entryTime);
        FollowingEdges.Add(followingEdge);

        CurrentEdge = FollowingEdges.FirstOrDefault();
        CurrentNode = CurrentEdge?.SourceNode;

        if (FollowingEdges.Count != 0)
        {
            TravelDuration = FollowingEdges.Last().EstimatedExitTime - CurrentEdge!.EntryTime;
        }
    }

    private void EstimateTimeForChargeAction(IReadOnlyList<string> nodes)
    {
        CurrentEdge = null;
        CurrentNode = _nodes.Single(e => e.Name == nodes.Single());

        // already there
        TravelDuration = TimeSpan.Zero;
    }

    private void EstimateTimeToGetBackToGraph(string targetNode, DateTime entryTime, AgvData agvData, double initialVelocity)
    {
        ResetAddedVirtualNodes(CurrentNode!.Name);
        var target = _nodes.Single(n => n.Name == targetNode);
        var graphEdge = GraphEdge.CreateDirect(CurrentNode.Id, targetNode.ToGuid(), (float) Mathematics.EuclideanDistance(CurrentNode, target), Guid.NewGuid(), _timeStamp);
        var edge = new DirectEdge(graphEdge, CurrentNode, target) { TimeStamp = _timeStamp };
        Edges.Add(edge);
        AddedVirtualEdges.Add(edge.Id);
        var followingEdge = edge;
        followingEdge.SetAngles(0, null, null);
        followingEdge.EstimateTravelTime(agvData, initialVelocity, entryTime);
        FollowingEdges.Add(followingEdge);
        CurrentEdge = FollowingEdges.FirstOrDefault();
        CurrentNode = CurrentEdge?.SourceNode;
        if (FollowingEdges.Count != 0)
        {
            TravelDuration = FollowingEdges.Last().EstimatedExitTime - CurrentEdge!.EntryTime;
        }
    }

    /// <inheritdoc />
    public void EstimateTravelTimes(AgvPosition targetPosition, DateTime entryTime, AgvData agvData, double initialVelocity)
    {
        string virtualSourceName = Guid.NewGuid().ToString();
        var virtualSource = new GraphNode(virtualSourceName.ToGuid(), virtualSourceName) { X = Position.X, Y = Position.Y, Theta = Position.Theta, TimeStamp = _timeStamp };
        string virtualTargetName = Guid.NewGuid().ToString();
        var virtualTarget = new GraphNode(virtualTargetName.ToGuid(), virtualSourceName) { X = targetPosition.X, Y = targetPosition.Y, Theta = targetPosition.Theta, TimeStamp = _timeStamp };
        _nodes.Add(virtualSource);
        AddedVirtualNodes.Add(virtualSource.Name);
        _nodes.Add(virtualTarget);
        AddedVirtualNodes.Add(virtualTarget.Name);

        var graphEdge = GraphEdge.CreateDirect(virtualSource.Id, virtualTarget.Id, (float) Mathematics.EuclideanDistance(virtualSource, virtualTarget), Guid.NewGuid(), _timeStamp);
        var edge = new DirectEdge(graphEdge, virtualSource, virtualTarget) { TimeStamp = _timeStamp };
        Edges.Add(edge);
        AddedVirtualEdges.Add(edge.Id);

        var followingEdge = edge;
        followingEdge.SetAngles(0, null, null);
        followingEdge.EstimateTravelTime(agvData, initialVelocity, entryTime);
        FollowingEdges.Add(followingEdge);

        CurrentEdge = FollowingEdges.FirstOrDefault();
        CurrentNode = CurrentEdge?.SourceNode;

        if (FollowingEdges.Count != 0)
        {
            TravelDuration = FollowingEdges.Last().EstimatedExitTime - CurrentEdge!.EntryTime;
        }
    }

    /// <param name="agvData"></param>
    /// <inheritdoc />
    public void UpdatePosition(AgvData agvData)
    {
        var currentTime = _timeProvider.Now();
        // deduct pause time from currentTime, so simulate the time as if the agv was not paused
        currentTime -= PauseTime;

        if (FollowingEdges.Count == 0)
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
        Position = CurrentEdge.GetCurrentPosition(currentTime, agvData, OrientationOnPreviousEdge);

        // Agv is at the end of the route
        if (Math.Abs(progress!.Value - 1d) < double.Epsilon)
        {
            CurrentEdgeDistance = (float) (CurrentEdge.Length / 1000);
            CurrentNode = CurrentEdge.TargetNode;
            Position = new AgvPosition(true, CurrentNode.X, CurrentNode.Y, CurrentNode.Z, GetOrientation(Position));
        }
    }

    private float GetOrientation(AgvPosition position)
    {
        bool isMoveToCoordinate = AddedVirtualNodes.Contains(CurrentEdge!.TargetNode.Name);
        bool isMoveBackToGraph = !AddedVirtualNodes.Contains(CurrentEdge!.TargetNode.Name) && AddedVirtualNodes.Contains(CurrentEdge.SourceNode.Name);

        if (isMoveToCoordinate || isMoveBackToGraph)
        {
            return CurrentEdge.TargetNode.Theta;
        }

        return position.Theta;
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
        foreach (string node in AddedVirtualNodes.Where(n => n != nodeToKeep))
        {
            _nodes.Remove(_nodes.Single(n => n.Name == node));
        }
        AddedVirtualNodes = AddedVirtualNodes.Except(AddedVirtualNodes.Where(n => n != nodeToKeep)).ToList();
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