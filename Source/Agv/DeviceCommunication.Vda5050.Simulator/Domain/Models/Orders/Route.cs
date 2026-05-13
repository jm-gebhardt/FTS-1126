using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.Domain.Models.Orders;

/// <summary>
/// Represents a route of an agv.
/// </summary>
[PublicAPI]
public class Route
{
    /// <summary>
    /// Represents a route of an agv.
    /// </summary>
    /// <param name="nodes">The nodes of the route.</param>
    /// <param name="edges">The edges of the route.</param>
    public Route(ICollection<OrderNode> nodes, ICollection<OrderEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }

    /// <summary>The nodes of the route.</summary>
    public ICollection<OrderNode> Nodes { get; private set; }

    /// <summary>The edges of the route.</summary>
    public ICollection<OrderEdge> Edges { get; private set; }

    /// <summary>
    /// TryProceed on the route.
    /// </summary>
    /// <param name="passedNodeSequenceId">The sequence id of the node that has been passed.</param>
    /// <returns>False, if there was no progress, or the passed node is not on this route.</returns>
    public void Proceed(int passedNodeSequenceId)
    {
        if (passedNodeSequenceId == 0)
        {
            // Ensure that start and end node of current edge are part of the route.
            return ;
        }
        Nodes = Nodes.Where(n => n.SequenceId >= passedNodeSequenceId).ToList();
        Edges = Edges.Where(e => e.SequenceId >= passedNodeSequenceId - 1).ToList();
    }

    /// <summary>
    /// Get the base of the route (released nodes and edges.)
    /// </summary>
    public Route GetBase()
    {
        return new Route(Nodes.Where(n => n.Released).OrderBy(n => n.SequenceId).ToList(), Edges.Where(e => e.Released).OrderBy(e => e.SequenceId).ToList());
    }
}