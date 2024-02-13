using Gebhardt.StoreWare.Agv.Common.Extensions;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Travel;

/// <summary>
/// Provides methods to simulate the agv travel on edges.
/// </summary>
public class AgvTravelSimulator
{
    private readonly List<GraphNode> _nodes = new();

    /// <summary>
    /// All edges of the agvs graph as <see cref="BaseEdge"/>.
    /// </summary>
    public List<BaseEdge> Edges { get; } = new();

    /// <summary>
    /// The edges the agv is about to travel on.
    /// </summary>
    public List<BaseEdge> FollowingEdges { get; private set; } = new();

    /// <summary>
    /// The edge the agv is currently traveling on.
    /// </summary>
    public BaseEdge? CurrentEdge { get; private set; }

    /// <summary>
    /// The current node of the agv.
    /// </summary>
    public GraphNode? CurrentNode { get; private set; }

    /// <summary>
    /// The agv data.
    /// </summary>
    private AgvData AgvData { get; } = null!;

    /// <summary>
    /// The travel duration.
    /// </summary>
    public TimeSpan? TravelDuration { get; private set; }

    /// <summary>
    /// The current position of the agv.
    /// </summary>
    public AgvPosition Position { get; private set; } = null!;

    /// <summary>
    /// Agv orientation on previous edge.
    /// </summary>
    private float OrientationOnPreviousEdge { get; set; }

    /// <inheritdoc cref="AgvTravelSimulator"/>>
    public AgvTravelSimulator(AgvGraph? graph, AgvData? data, AgvPosition position)
    {
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

        Position = position;
        AgvData = data!;
    }

    /// <summary>
    /// Estimates travel times on the current edges.
    /// </summary>
    public void EstimateTravelTimes(IReadOnlyList<string> nodes, DateTime entryTime, double initialVelocity)
    {
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

        foreach (var node in nodes.Select((id, index) => new { id, index }))
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
            edge.EstimateTravelTime(AgvData, initialVelocity, entryTime);
            entryTime = edge.EstimatedExitTime;
            initialVelocity = edge.ExitVelocity;
            entryAngle = edge.ExitAngle;
            FollowingEdges.Add(edge);
        }
        edge = Edges.Single(e => e.FromNodeId == nodes[^2] && e.ToNodeId == nodes[^1]);
        edge.SetAngles(entryAngle, null, null);
        edge.EstimateTravelTime(AgvData, initialVelocity, entryTime);
        FollowingEdges.Add(edge);

        CurrentEdge = FollowingEdges.FirstOrDefault();
        CurrentNode = CurrentEdge?.SourceNode;

        if (FollowingEdges.Any())
        {
            TravelDuration = FollowingEdges.Last().EstimatedExitTime - CurrentEdge!.EntryTime;
        }
    }

    /// <summary>
    /// Updates the current position.
    /// </summary>
    /// <param name="currentTime"></param>
    public void UpdatePosition(DateTime currentTime)
    {
        if (!FollowingEdges.Any())
        {
            return;
        }

        // limit currentTime to the estimated exit time of the last edge, as the agv does not move further.
        currentTime = currentTime > FollowingEdges.Last().EstimatedExitTime ? FollowingEdges.Last().EstimatedExitTime : currentTime;
        CurrentEdge = GetCurrentEdge(currentTime);
        double? progress = (currentTime - FollowingEdges.First().EntryTime) / TravelDuration;
        CurrentNode = CurrentEdge.SourceNode;
        Position = CurrentEdge.GetCurrentPosition(currentTime, AgvData, OrientationOnPreviousEdge);

        // Agv is at the end of the route
        if (Math.Abs(progress!.Value - 1d) < double.Epsilon)
        {
            float theta = Position.Theta;
            // quick turn at station, as our Agv are not yet capable of reversing  
            if (CurrentEdge.TargetNode.IsStation())
            {
                theta -= (float)Math.PI;
            }
            CurrentNode = CurrentEdge.TargetNode;
            Position = new AgvPosition(true, CurrentNode.X, CurrentNode.Y, CurrentNode.Z, theta);
        }
    }

    /// <summary>
    /// Finishes edge travel.
    /// </summary>
    public void FinishTravel()
    {
        FollowingEdges = new List<BaseEdge>();
        CurrentEdge = null;
        TravelDuration = null;
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

    /// <summary>
    /// Get the shortest edge of the current route.
    /// </summary>
    /// <returns></returns>
    public BaseEdge GetShortestEdge()
    {
        return FollowingEdges.MinBy(e => e.EstimatedExitTime - e.InitialTurnFinishTime) ?? CurrentEdge!;
    }
}