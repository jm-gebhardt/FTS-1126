using System.Numerics;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Base;
using Gebhardt.StoreWare.Agv.TransportOrder.Routing.Models;
using JetBrains.Annotations;

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
            G = 0f;
            H = 0f;
        }

        /// <summary>
        /// Predecessor node ID of the calculated path.
        /// </summary>
        public string Predecessor { get; set; } = "";

        /// <summary>
        /// Distance (metric/costs) from start to this node.
        /// </summary>
        public float G { get; set; }

        /// <summary>
        /// Remaining (estimated) distance (metric/costs) to the destination.
        /// </summary>
        public float H { get; set; }

        /// <summary>
        /// (Estimation of) Costs(metric) from start to destination.
        /// </summary>
        public float F => G + H;

        /// <summary>
        /// Clone <see cref="Node"/>.
        /// </summary>
        /// <param name="node"></param>
        public ANode(GraphNode node)
        {
            Node = node;
        }

        /// <summary>
        /// Simple string representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Node.Id} [{Predecessor}, F({F}) = G({G}) + H({H})]";
    }

    /// <summary>List of nodes currently processing.</summary>
    private Dictionary<string, ANode> _openList = new(); // Should be a PriorityQueue<node, float> but we need to update the sorting attribute.

    /// <summary>List of nodes already checked.</summary>
    private SortedSet<string> _closedList = new();

    private readonly Dictionary<string, ANode> _nodes;

    /// <summary>
    /// A* implementation.
    /// </summary>
    /// <param name="agvGraph"></param>
    /// <param name="routingPlausibilityChecker">The checker to use for the check in the base implementation</param>
    public AStar(AgvGraph agvGraph, IRoutingPlausibilityChecker? routingPlausibilityChecker = null) : base(agvGraph, routingPlausibilityChecker)
    {
        _nodes = agvGraph.Nodes.ToDictionary(n => n.Id, n => new ANode(n));
    }

    /// <summary>
    /// Calculate distance between two nodes.
    /// </summary>
    /// <param name="first">One ot the nodes.</param>
    /// <param name="second">The other node.</param>
    /// <returns>Returns the euclidean distance.</returns>
    private float EuclideanDistance(ANode first, ANode second)
    {
        var vectorStart = new Vector3(first.Node.X, first.Node.Y, first.Node.Z);
        var vectorEnd = new Vector3(second.Node.X, second.Node.Y, second.Node.Z);

        return (vectorStart - vectorEnd).Length();
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

            float g = current.G + edge.Metric;
            var successor = _nodes[edge.ToNodeId];

            if (_openList.ContainsKey(successor.Node.Id) && g >= successor.G)
            {
                continue;
            }

            successor.Predecessor = current.Node.Id;
            successor.G = g;
            if (successor.H == 0f && successor != destination)
            {
                // Estimate distance to final destination.
                successor.H = EuclideanDistance(successor, destination);
            }

            _openList.TryAdd(successor.Node.Id, successor);
        }
    }

    /// <inheritdoc/>>
    protected override Route? CalculateRouteInternal(string source, string destination)
    {
        _openList.Clear();
        _closedList.Clear();
        _nodes.Values.ToList().ForEach(x => x.Reset());

        var start = _nodes[source];
        var end = _nodes[destination];

        // Estimate distance to final destination.
        start.H = EuclideanDistance(start, end);

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

        if (end.Predecessor != "")
        {
            var route = new List<string> {end.Node.Id};
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
        // No path found.
        return null;
    }
}