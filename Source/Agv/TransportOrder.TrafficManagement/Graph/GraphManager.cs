using System.Diagnostics;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Edge = Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models.Edge;
using Node = Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models.Node;
using Station = Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models.Station;
using CommonGraph = Gebhardt.Acc.Common.Domain.Models.NewGraph.Graph;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph;

[PublicAPI]
internal class GraphManager(INewGraphRepository newGraphRepository, IAgvTypeRepository agvTypeRepository, ILogger<GraphManager> logger)
{
    private readonly Dictionary<Guid, Node> _nodes = [];
    private readonly Dictionary<Guid, Edge> _edges = [];
    private readonly Dictionary<Guid, Station> _stations = [];

    public IEnumerable<Node> Nodes => _nodes.Values;

    public IEnumerable<Edge> Edges => _edges.Values;

    public IEnumerable<Station> Stations => _stations.Values;

    public Node? GetNodeByShortName(string? targetNodeShortName)
    {
        return _nodes.Values.FirstOrDefault(n => n.ShortName == targetNodeShortName);
    }

    public Node? GetNodeById(Guid nodeId)
    {
        return _nodes.GetValueOrDefault(nodeId);
    }

    /// <summary>
    /// Loads the stored graph into the internal representation of nodes, edges and stations.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var initializationWatch = Stopwatch.StartNew();

        var graph = await newGraphRepository.GetGraphAsync();
        
        await AddGraphSegmentAsync(graph, cancellationToken);

        initializationWatch.Stop();
        logger.LogDebug("GraphManager initialized in {ElapsedMilliseconds} ms.", initializationWatch.ElapsedMilliseconds);
    }

    /// <summary>
    /// Adds a graph segment to the graph. This method converts the common graph segment into the internal representation of nodes, edges, and stations, and adds them to the respective dictionaries.
    /// </summary>
    public async Task AddGraphSegmentAsync(CommonGraph graph, CancellationToken cancellationToken)
    {
        ICollection<AgvType> allAgvTypes = await agvTypeRepository.GetAllAgvTypesAsync(cancellationToken); //todo: reuse Types (from AgvProvider? or store agvTypes in GraphManager?)
        Dictionary<Guid, AgvType> agvTypes = allAgvTypes.ToDictionary(a => a.Id);

        var mappingWatch = Stopwatch.StartNew();

        (IReadOnlyCollection<Node> nodes, IReadOnlyCollection<Edge> edges, IReadOnlyCollection<Station> stations) = GraphMapper.Convert(graph, agvTypes);

        // Add nodes, edges, and stations to the dictionaries with Add() so that duplicated Ids throw an exception. At the moment, Ids are unique, because updates are not supported.

        foreach (var node in nodes)
        {
            _nodes.Add(node.Id, node);
        }

        foreach (var edge in edges)
        {
            _edges.Add(edge.Id, edge);
        }

        foreach (var station in stations)
        {
            _stations.Add(station.Id, station);
        }

        mappingWatch.Stop();

        int routingNodeCount = nodes.SelectMany(n => n.RoutingNodes).Count();
        int routingEdgeCount = nodes.SelectMany(n => n.RoutingEdges).Count() + edges.SelectMany(e => e.RoutingEdges).Count();

        logger.LogInformation("Graph segment added: {NodeCount} nodes, {EdgeCount} edges, {StationCount} stations, {RoutingNodeCount} routing nodes, {RoutingEdgeCount} routing edges " +
                               "(mapping: {ElapsedMilliseconds} ms).", nodes.Count, edges.Count, stations.Count, routingNodeCount, routingEdgeCount, mappingWatch.ElapsedMilliseconds);
    }
}