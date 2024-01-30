using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using Gebhardt.StoreWare.Agv.Common.Utilities;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Base;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Routing.Implementations;

/// <summary>
/// A* algorithm.
/// </summary>
[PublicAPI]
public class AStar : BaseRouteAlgorithm
{
    /// <summary>
    /// Node extended with properties required for A* calculation.
    /// </summary>
    /// <remarks>
    /// All nodes (x) get a metric (costs) f(x)
    /// that is the best path from start to this node g(x)
    /// plus an estimation from this node to the destination h(x).
    /// <para>
    /// f(x) = g(x) + h(x)
    /// </para><para>
    /// <see cref="F"/> = <see cref="G"/> + <see cref="H"/>
    /// </para></remarks>
    private class ANode
    {
        /// <summary>
        /// Node representation.
        /// </summary>
        public GraphNode Node { get; }

        /// <summary>
        /// Reset members used for path calculation.
        /// </summary>
        public void Reset()
        {
            Predecessor = "";
            G = 0;
            H = 0;
            EntryVector = null;
            EntryVelocity = 0;
            DecelerationCost = 0;
        }

        /// <summary>
        /// The entry vector into the node.
        /// </summary>
        public Vector2? EntryVector { get; set; }

        /// <summary>
        /// The velocity [m/s] that the agv enters the node with.
        /// </summary>
        public double EntryVelocity { get; set; }

        /// <summary>
        /// The cost to decelerate [s] on the edge that enters this node.
        /// </summary>
        public double DecelerationCost { get; set; }

        /// <summary>
        /// Predecessor node ID of the calculated path.
        /// </summary>
        public string Predecessor { get; set; } = "";

        /// <summary>
        /// Time from start to this node.
        /// </summary>
        public double G { get; set; }

        /// <summary>
        /// Remaining (estimated) time [s] to the destination at maximum velocity.
        /// </summary>
        public double H { get; set; }

        /// <summary>
        /// (Estimation of) Costs(metric) from start to destination.
        /// </summary>
        public double F => G + H;

        /// <summary>
        /// Clone <see cref="Node"/>.
        /// </summary>
        /// <param name="node"></param>
        public ANode(GraphNode node)
        {
            Node = node;
            DecelerationCost = 0;
            EntryVelocity = 0;
            EntryVector = null;
        }

        /// <summary>
        /// Simple string representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{Node.Id} [{Predecessor}, {EntryVector}, F({F}) = G({G}) + H({H})]";
        }

        public Vector2 AsVector2()
        {
            return new Vector2(Node.X, Node.Y);
        }

        /// <summary>
        /// Calculates the distance [m] from this <see cref="ANode"/> to the target node.
        /// </summary>
        /// <param name="target">The target <see cref="ANode"/></param>
        public double DistanceTo(ANode target)
        {
            return Mathematics.EuclideanDistance(Node, target.Node);
        }
    }

    /// <summary>List of nodes currently processing.</summary>
    private readonly Dictionary<string, ANode> _openList = new(); // Should be a PriorityQueue<node, float> but we need to update the sorting attribute.

    /// <summary>List of nodes already checked.</summary>
    private readonly SortedSet<string> _closedList = new();

    private readonly Dictionary<string, ANode> _nodes;

    private readonly AgvData? _agvData;

    private double MaxVelocityTurnOnSpot => _agvData?.MaxVelocityTurnOnSpot ?? MaxVelocityTurnOnSpotFallback;

    private double TurnOnSpotThreshold => _agvData?.TurnOnSpotThreshold ?? MaxNonSpotTurnAngleFallback;

    private double Acceleration => _agvData?.Acceleration ?? AccelerationFallback;

    private double MaxVelocityStraight => _agvData?.MaxVelocityStraight ?? MaxVelocityStraightFallback;

    private double MaxVelocityTurn => _agvData?.MaxVelocityTurn ?? MaxVelocityTurnFallback;

    /// <summary>
    /// A* implementation.
    /// </summary>
    /// <param name="agvGraph"></param>
    /// <param name="agvData"></param>
    /// <param name="routingPlausibilityChecker">The checker to use for the check in the base implementation</param>
    public AStar(IGraph agvGraph, AgvData? agvData = null, IRoutingPlausibilityChecker? routingPlausibilityChecker = null) : base(agvGraph, routingPlausibilityChecker)
    {
        _nodes = agvGraph.Nodes.ToDictionary(n => n.Id, n => new ANode(n));
        _agvData = agvData;
    }

    /// <summary>
    /// Helper to find and remove node from <see cref="_openList"/> with least costs.
    /// <para>
    /// We do not use LINQ because iteration is faster and less garbage.
    /// </para></summary>
    /// <returns>Returns the removed node with the minimal cost value or null when <see cref="_openList"/> is empty.</returns>
    private ANode? GetAndRemoveMin()
    {
        ANode? min = null;
        foreach (KeyValuePair<string, ANode> kvp in _openList)
        {
            if (min == null || kvp.Value.F < min.F)
            {
                min = kvp.Value;
            }
        }

        if (min != null)
        {
            _openList.Remove(min.Node.Id);
        }
        return min;
    }

    /// <summary>
    /// Main function of the A* algorithm.
    /// </summary>
    /// <param name="current"></param>
    /// <param name="destination"></param>
    private void ExpandNode(ANode current, ANode destination)
    {
        foreach (var edge in Edges.Where(x => x.FromNodeId == current.Node.Id))
        {
            if (_closedList.Any(x => x == edge.ToNodeId))
            {
                continue;
            }

            double time = CalculateCosts(current, edge, out double finalVelocity, out double decelerationCost);

            double g = current.G + time;
            var successor = _nodes[edge.ToNodeId];

            if (_openList.ContainsKey(successor.Node.Id) && g >= successor.G)
            {
                continue;
            }

            successor.Predecessor = current.Node.Id;
            successor.EntryVector = CalculateExitVector(edge);
            successor.DecelerationCost = decelerationCost;
            successor.EntryVelocity = finalVelocity;
            successor.G = g;

            if (successor.H == 0f && successor != destination)
            {
                // Estimated time to final destination at maximum velocity.
                successor.H = successor.DistanceTo(destination) / MaxVelocityStraight;
            }

            _openList.TryAdd(successor.Node.Id, successor);
        }
    }

    /// <summary>
    /// Calculates the cost [s] of moving on an edge.
    /// </summary>
    /// <param name="current">The current <see cref="ANode"/> that we want to move from.</param>
    /// <param name="edge">The current edge that we want to move on.</param>
    /// <param name="finalVelocity">The velocity [m/s] at the end of the edge.</param>
    /// <param name="decelerationCost">The additional cost [s] that has to be factored if we would have to brake on this edge.</param>
    private double CalculateCosts(ANode current, GraphEdge edge, out double finalVelocity, out double decelerationCost)
    {
        double time = 0;

        double maximumEdgeVelocity = CalculateMaximumEdgeVelocity(edge);

        time += CalculateEntryCost(current, edge, maximumEdgeVelocity, out double actualEntryVelocity);

        time += edge.Variant switch
        {
            EdgeType.Direct => CalculateTimeAndExitParametersForDistance(edge.Length, actualEntryVelocity, maximumEdgeVelocity, Acceleration, out finalVelocity, out decelerationCost),
            EdgeType.Bezier1 or EdgeType.Bezier2 => CalculateTimeAndExitParametersForDistance(edge.Length, actualEntryVelocity, maximumEdgeVelocity, Acceleration, out finalVelocity, out decelerationCost),
            _ => CalculateTimeAndExitParametersForDistance(edge.Length, actualEntryVelocity, maximumEdgeVelocity, Acceleration, out finalVelocity, out decelerationCost)
        };

        return time;
    }

    /// <summary>
    /// Calculates the maximum edge velocity [m/s].
    /// </summary>
    /// <param name="edge">The edge.</param>
    private double CalculateMaximumEdgeVelocity(GraphEdge edge)
    {
        return edge.Variant switch
        {
            EdgeType.Direct => MaxVelocityStraight,
            EdgeType.Bezier1 or EdgeType.Bezier2 => MaxVelocityTurn,
            _ => MaxVelocityStraight
        };
    }

    /// <summary>
    /// Calculates if the Agv has to perform a spot turn and how much that spot turn costs (0 if no spot turn necessary).
    /// </summary>
    /// <param name="current">The current <see cref="ANode"/> that we want to move from.</param>
    /// <param name="edge">The edge that we want to move on.</param>
    /// <param name="maximumEdgeVelocity">The maximum velocity that should be reached on this edge.</param>
    /// <param name="actualEntryVelocity">The actual velocity that the Agv has at the start of the edge. (0 if we have to do a spot turn.)</param>
    private double CalculateEntryCost(ANode current, GraphEdge edge, double maximumEdgeVelocity, out double actualEntryVelocity)
    {
        double time = 0;
        double angle = 0;

        if (current.EntryVector != null)
        {
            angle = Mathematics.AngleBetween((Vector2)current.EntryVector, CalculateEntryVector(edge));
        }

        bool isFirstNode = current.Predecessor.IsNullOrEmpty();

        bool needsSpotTurn = angle > TurnOnSpotThreshold || isFirstNode;

        actualEntryVelocity = needsSpotTurn ? 0d : current.EntryVelocity;

        // Interpolation to figure out how much slower we need to go
        if (actualEntryVelocity > maximumEdgeVelocity)
        {
            time += current.DecelerationCost * actualEntryVelocity / maximumEdgeVelocity;
            actualEntryVelocity = maximumEdgeVelocity;
        }

        if (!needsSpotTurn)
        {
            return time;
        }

        // Perform the spot turn.
        time += angle / MaxVelocityTurnOnSpot;

        if (!isFirstNode)
        {
            // We needed to decelerate on the previous edge too
            time += current.DecelerationCost;
        }
        return time;
    }

    private static double CalculateTimeAndExitParametersForDistance(double distance, double actualEntryVelocity, double maxVelocity, double acceleration, out double finalVelocity, out double decelerationCost)
    {
        PhysicsHelper.CalculateAccelerateMaxVelocityDecelerateSections(acceleration, distance, maxVelocity, actualEntryVelocity, out double accelerateSection, out double maxVelocitySection, out double decelerateSection);

        double time = PhysicsHelper.CalculateTimeToAccelerateAndMoveAtMaxVelocity(acceleration, distance, maxVelocity, actualEntryVelocity, out finalVelocity);

        double accelerationTimeOnEdgeWithBraking = PhysicsHelper.CalculateTimeToAccelerateAndMoveAtMaxVelocity(acceleration, accelerateSection + maxVelocitySection, maxVelocity, actualEntryVelocity);

        // Calculated from the time to reach the maximum velocity possible on the edge (could be that we have to decelerate before reaching max) + deceleration time - the time it takes without decelerating
        decelerationCost = accelerationTimeOnEdgeWithBraking
                           + PhysicsHelper.TimeToAccelerateFromStanding(acceleration, decelerateSection)
                           - time;
        return time;
    }

    /// <summary>
    /// Calculates the exit vector out of the ToNode.
    /// Note: for direct and slanted these are identical to <see cref="CalculateEntryVector"/>.
    /// </summary>
    /// <param name="edge">The edge.</param>
    private Vector2 CalculateExitVector(GraphEdge edge)
    {
        var fromNodeVector = _nodes[edge.FromNodeId].AsVector2();
        var toNodeVector = _nodes[edge.ToNodeId].AsVector2();
        return edge.Variant switch
        {
            EdgeType.Direct => toNodeVector - fromNodeVector,
            EdgeType.Bezier1 or EdgeType.Bezier2 => toNodeVector - edge.VariantParameters.Last().ToVector2(),
            // Fallback is Direct
            _ => toNodeVector - fromNodeVector
        };
    }

    /// <summary>
    /// Calculates the entry vector into the FromNode.
    /// Note: for direct and slanted these are identical to <see cref="CalculateExitVector"/>.
    /// </summary>
    /// <param name="edge">The edge.</param>
    private Vector2 CalculateEntryVector(GraphEdge edge)
    {
        var fromNodeVector = _nodes[edge.FromNodeId].AsVector2();
        var toNodeVector = _nodes[edge.ToNodeId].AsVector2();
        return edge.Variant switch
        {
            EdgeType.Direct => toNodeVector - fromNodeVector,
            EdgeType.Bezier1 or EdgeType.Bezier2 => edge.VariantParameters.First().ToVector2() - fromNodeVector,
            // Fallback is Direct
            _ => toNodeVector - fromNodeVector
        };
    }

    /// <inheritdoc/>>
    protected override Route? CalculateRouteInternal(string source, string destination)
    {
        _openList.Clear();
        _closedList.Clear();
        _nodes.Values.ToList().ForEach(x => x.Reset());

        var start = _nodes[source];
        var end = _nodes[destination];

        // Estimate time to final destination at maximum velocity.
        start.H = start.DistanceTo(end) / MaxVelocityStraight;

        ANode? current;
        _openList.Add(start.Node.Id, start);

        do
        {
            current = GetAndRemoveMin();
            if (current == end)
            {
                // Found
                break;
            }

            _closedList.Add(current!.Node.Id);

            ExpandNode(current, end);
        } while (_openList.Count != 0);

        if (end.Predecessor.IsNullOrEmpty())
        {
            // No path found.
            return null;
        }
        var route = new List<string> { end.Node.Id };
        current = end;

        // Write path found.
        while (current.Node.Id != source)
        {
            current = _nodes[current.Predecessor];
            route.Add(current.Node.Id);
        }

        route.Reverse();

        return new Route(end.F, route.ToArray());
    }
}