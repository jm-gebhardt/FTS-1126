using AutoMapper;
using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.NewGraph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.WebApi.Profiles.Converter;

/// <summary>
/// Converts the graph into its DTO. Each AGV state is listed once and referenced by index. The routing edges of an
/// edge are grouped into its distinct trajectories.
/// </summary>
[UsedImplicitly]
public class GraphDtoConverter : ITypeConverter<(Graph Graph, IReadOnlyList<LoadState> LoadStates), GraphDto>
{
    private static readonly NodeStatusDto _unknownNodeStatus = new(OccupancyState.Free, AgvId: null, Lock: null, ModifiedDate: null);
    private static readonly StationStatusDto _unknownStationStatus = new(Lock: null, LockedOrderHandling: null, HasPalletJam: false, IsOnline: null, ModifiedDate: null);

    private readonly record struct AgvStateKey(Guid AgvTypeId, Guid LoadStateId);

    private static List<int> ToIndices(IEnumerable<AgvStateKey> keys, Dictionary<AgvStateKey, int> indexByKey)
    {
        return keys.Select(k => indexByKey[k]).Distinct().Order().ToList();
    }

    private static Dictionary<Guid, AgvStateKey> GetRoutingNodeKeys(Graph graph)
    {
        return graph.Nodes
            .SelectMany(n => n.RoutingNodes)
            .ToDictionary(r => r.Id, r => new AgvStateKey(r.AgvTypeId, r.LoadStateId));
    }

    private static AgvStateKey? GetRoutingEdgeKey(RoutingEdge routingEdge, Dictionary<Guid, AgvStateKey> routingNodeKeys)
    {
        return routingNodeKeys.TryGetValue(routingEdge.StartNodeId, out var startNodeKey) ? startNodeKey : null;
    }

    private static TrajectoryDto ToTrajectoryDto(Trajectory? trajectory, List<int> agvStateIndices)
    {
        if (trajectory is null || trajectory.ControlPoints.Count == 0)
        {
            return new TrajectoryDto(agvStateIndices, [], Degree: null, []);
        }

        var controlPoints = trajectory.ControlPoints.Select(c => new ControlPointDto(c.X, c.Y, c.Weight)).ToList();
        return new TrajectoryDto(agvStateIndices, controlPoints, (int?)trajectory.Degree, trajectory.KnotVector);
    }

    private static List<RoutingActionType> GetCapabilities(Node node)
    {
        return node.RoutingNodes.SelectMany(r => r.Actions).Distinct().Order().ToList();
    }

    private static StationDto ToStationDto(Station station, Dictionary<Guid, NodeDto> nodesById)
    {
        var capabilities = station.InteractionNodeIds
            .Select(nodesById.GetValueOrDefault)
            .OfType<NodeDto>()
            .SelectMany(n => n.Capabilities)
            .Distinct()
            .Order()
            .ToList();

        return new StationDto(station.Id, station.Name, station.ShortName, station.X, station.Y, station.Theta, station.StationHeight, station.InteractionNodeIds,
            capabilities, _unknownStationStatus);
    }

    private static EdgeDto ToEdgeDto(Edge edge, Dictionary<Guid, AgvStateKey> routingNodeKeys, Dictionary<AgvStateKey, int> indexByKey)
    {
        var trajectories = edge.RoutingEdges
            .Select(r => (r.Trajectory, Key: GetRoutingEdgeKey(r, routingNodeKeys)))
            .Where(r => r.Key.HasValue)
            .GroupBy(r => r.Trajectory, TrajectoryComparer.Instance)
            .Select(group => ToTrajectoryDto(group.Key, ToIndices(group.Select(r => r.Key!.Value), indexByKey)))
            .OrderBy(t => t.AgvStateIndices[0])
            .ToList();
        var agvStateIndices = trajectories.SelectMany(t => t.AgvStateIndices).Distinct().Order().ToList();

        return new EdgeDto(edge.Id, edge.Name, edge.ShortName, edge.StartNodeId, edge.EndNodeId, agvStateIndices, trajectories);
    }

    /// <inheritdoc/>
    public GraphDto Convert((Graph Graph, IReadOnlyList<LoadState> LoadStates) source, GraphDto destination, ResolutionContext context)
    {
        var (graph, loadStates) = source;
        var isLoadedById = loadStates.ToDictionary(l => l.Id, l => l.IsLoaded);
        var routingNodeKeys = GetRoutingNodeKeys(graph);

        var keys = routingNodeKeys.Values
            .Concat(graph.Edges.SelectMany(e => e.RoutingEdges).Select(r => GetRoutingEdgeKey(r, routingNodeKeys)).OfType<AgvStateKey>())
            .Distinct()
            .OrderBy(k => k.AgvTypeId)
            .ThenBy(k => k.LoadStateId)
            .ToList();
        var indexByKey = keys.Select((key, index) => (key, index)).ToDictionary(k => k.key, k => k.index);

        var agvStates = keys
            .Select(k => new AgvTypeStateDto(k.AgvTypeId, new LoadStateDto(k.LoadStateId, isLoadedById.GetValueOrDefault(k.LoadStateId))))
            .ToList();
        var nodes = graph.Nodes
            .Select(n => new NodeDto(n.Id, n.Name, n.ShortName, n.X, n.Y, n.LevelId, ToIndices(n.RoutingNodes.Select(r => routingNodeKeys[r.Id]), indexByKey), GetCapabilities(n), _unknownNodeStatus))
            .ToList();
        var edges = graph.Edges.Select(e => ToEdgeDto(e, routingNodeKeys, indexByKey)).ToList();
        var nodesById = nodes.ToDictionary(n => n.Id);
        var stations = graph.Stations.Select(s => ToStationDto(s, nodesById)).ToList();

        return new GraphDto(nodes, edges, stations, agvStates);
    }
}