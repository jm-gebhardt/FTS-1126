using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;
using Microsoft.Extensions.Logging;
using CommonGraph = Gebhardt.Acc.Common.Domain.Models.NewGraph.Graph;
using Edge = Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models.Edge;
using Node = Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models.Node;
using Station = Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models.Station;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph;

internal class GraphProvider(INewGraphRepository newGraphRepository, IAgvTypeRepository agvTypeRepository, ILogger<GraphProvider> logger)
{
    private readonly Dictionary<Guid, AgvType> _agvTypes = [];
    private readonly Dictionary<Guid, Node> _nodes = [];
    private readonly Dictionary<Guid, Edge> _edges = [];
    private readonly Dictionary<Guid, Station> _stations = [];

    public IEnumerable<Node> Nodes => _nodes.Values;

    public IEnumerable<Edge> Edges => _edges.Values;

    public IEnumerable<Station> Stations => _stations.Values;

    public bool TryGetAgvType(Guid agvTypeId, [NotNullWhen(true)] out AgvType? agvType)
    {
        return _agvTypes.TryGetValue(agvTypeId, out agvType);
    }

    public Node? GetNodeByShortName(string? nodeShortName)
    {
        return _nodes.Values.FirstOrDefault(n => n.ShortName == nodeShortName);
    }

    public Node? GetNodeById(Guid nodeId)
    {
        return _nodes.GetValueOrDefault(nodeId);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var initializationWatch = Stopwatch.StartNew();

        await AddOrUpdateAgvTypesAsync(cancellationToken);

        var graph = await newGraphRepository.GetGraphAsync(cancellationToken);
        
        AddGraphSegment(graph);

        initializationWatch.Stop();
        logger.LogDebug("GraphManager initialized in {ElapsedMilliseconds} ms.", initializationWatch.ElapsedMilliseconds);
    }

    public async Task AddOrUpdateAgvTypesAsync(CancellationToken cancellationToken)
    {
        var agvTypes = await agvTypeRepository.GetAllAgvTypesAsync(cancellationToken);

        foreach (var agvType in agvTypes)
        {
            if (_agvTypes.TryGetValue(agvType.Id, out var existingAgvType))
            {
                existingAgvType.Manufacturer = agvType.Manufacturer;
                existingAgvType.SeriesName = agvType.SeriesName;
                existingAgvType.Length = agvType.Length;
                existingAgvType.Width = agvType.Width;
                existingAgvType.Acknowledged = agvType.Acknowledged;
                existingAgvType.ReportedByAgv = agvType.ReportedByAgv;
                existingAgvType.RawData = agvType.RawData;
            }
            else
            {
                _agvTypes.Add(agvType.Id, agvType);
            }
        }
    }

    public void AddGraphSegment(CommonGraph graph)
    {
        var (nodes, edges, stations) = GraphMapper.Convert(graph, _agvTypes);

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

        int routingNodeCount = nodes.SelectMany(n => n.RoutingNodes).Count();
        int routingEdgeCount = nodes.SelectMany(n => n.RoutingEdges).Count() + edges.SelectMany(e => e.RoutingEdges).Count();

        logger.LogInformation("Graph segment added: {NodeCount} nodes, {EdgeCount} edges, {StationCount} stations, {RoutingNodeCount} routing nodes, {RoutingEdgeCount} routing edges.", 
            nodes.Count, edges.Count, stations.Count, routingNodeCount, routingEdgeCount);
    }
}