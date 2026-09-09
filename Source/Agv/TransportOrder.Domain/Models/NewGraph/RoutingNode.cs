using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// A node in the routing graph. Each routing node has an associated AGV type, a load state, and a theta value.
/// </summary>
public class RoutingNode : RoutingResource
{
    [UsedImplicitly]
    private readonly List<RoutingEdge> _incomingEdges = [];
    
    [UsedImplicitly]
    private readonly List<RoutingEdge> _outgoingEdges = [];

    /// <summary>
    /// The load state of the routing node.
    /// </summary>
    [UsedImplicitly]
    public LoadState LoadState { get; }

    /// <summary>
    /// The theta value of the routing node, which may represent the orientation of the AGV.
    /// </summary>
    [UsedImplicitly]
    public double Theta { get; }

    /// <summary>
    /// Collection of incoming edges to this routing node.
    /// </summary>
    [UsedImplicitly]
    public IReadOnlyCollection<RoutingEdge> IncomingEdges => _incomingEdges;

    /// <summary>
    /// Collection of outgoing edges from this routing node.
    /// </summary>
    [UsedImplicitly]
    public IReadOnlyCollection<RoutingEdge> OutgoingEdges => _outgoingEdges;

    /// <summary>
    /// The parent node of this routing node.
    /// </summary>
    public Node ParentNode => (Node)Parent;

    /// <inheritdoc cref="RoutingNode"/>
    public RoutingNode(Guid id, Node parent, AgvType agvType, LoadState loadState, double theta) : base(id, parent, agvType)
    {
        LoadState = loadState;
        Theta = theta;
    }

    /// <summary>
    /// Required by EF Core for materializing RoutingNode instances from the database.
    /// </summary>
    public RoutingNode()
    {
        LoadState = null!;
    }

    internal void AddIncomingEdge(RoutingEdge routingEdge)
    {
        _incomingEdges.Add(routingEdge);
    }

    internal void AddOutgoingEdge(RoutingEdge routingEdge)
    {
        _outgoingEdges.Add(routingEdge);    
    }
}