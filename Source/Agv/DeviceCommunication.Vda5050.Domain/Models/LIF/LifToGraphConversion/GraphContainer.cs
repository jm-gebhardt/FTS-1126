using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using LifNode = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif.Node;
using LifEdge = Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif.Edge;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.LIF.LifToGraphConversion;

/// <summary>
/// Mutable container used while converting a LIF layout into a <see cref="Common.Domain.Models.NewGraph.GraphSegment"/>.
/// Collects nodes, edges and stations as they are created, deduplicates routing nodes and resolves AGV type identifiers by vehicle type id.
/// </summary>
internal class GraphContainer(IReadOnlyDictionary<string, Guid> agvTypeIdentifiers)
{
    public required string LifId { get; init; }

    private readonly Dictionary<Guid, Node> _nodes = new();
    private readonly Dictionary<Guid, Edge> _edges = new();
    private readonly Dictionary<Guid, Station> _stations = new();

    private readonly Dictionary<Guid, NodeHelper> _nodeHelpers = new();
    private readonly List<EdgeHelper> _edgeHelpers = new();

    public Node[] GetNodes()
    {
        return [.. _nodes.Values];
    }

    public Edge[] GetEdges()
    {
        return [.. _edges.Values];
    }

    public Station[] GetStations()
    {
        return [.. _stations.Values];
    }

    public void Add(LifNode lifNode, Node node)
    {
        _nodes.Add(node.Id, node);
        _nodeHelpers.Add(node.Id, new NodeHelper(lifNode));
    }

    public void Add(LifEdge lifEdge, Edge edge)
    {
        _edges.Add(edge.Id, edge);

        var edgeHelper = new EdgeHelper(lifEdge);

        _edgeHelpers.Add(edgeHelper);
        _nodeHelpers[edge.StartNodeId].OutgoingEdges.Add(edgeHelper);
        _nodeHelpers[edge.EndNodeId].IncomingEdges.Add(edgeHelper);
    }

    public void Add(Station station)
    {
        _stations.Add(station.Id, station);
    }

    public bool TryGetAgvTypeId(string vehicleTypeId, out Guid agvTypeId)
    {
        return agvTypeIdentifiers.TryGetValue(vehicleTypeId, out agvTypeId);
    }

    public void Add(Guid parentNodeId, RoutingNode routingNode)
    {
        _nodes[parentNodeId].RoutingNodes.Add(routingNode);
    }

    public void Add(Guid parentEdgeId, RoutingEdge routingEdge)
    {
        _edges[parentEdgeId].RoutingEdges.Add(routingEdge);
    }

    public IEnumerable<NodeHelper> GetNodeHelpers()
    {
        return _nodeHelpers.Values;
    }

    public NodeHelper GetNodeHelper(Guid nodeId)
    {
        return _nodeHelpers[nodeId];
    }

    public IEnumerable<EdgeHelper> GetEdgeHelpers()
    {
        return _edgeHelpers;
    }

    public IReadOnlyCollection<RoutingNode> GetRoutingNodes(Guid nodeId, Guid agvTypeId, Guid loadStateId)
    {
        return _nodes[nodeId].RoutingNodes.Where(rn => rn.AgvTypeId == agvTypeId && rn.LoadStateId == loadStateId).ToList();
    }
}