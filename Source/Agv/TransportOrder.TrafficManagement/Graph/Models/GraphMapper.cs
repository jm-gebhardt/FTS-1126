using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

using CommonGraph = Common.Domain.Models.NewGraph.Graph;
using CommonNode = Common.Domain.Models.NewGraph.Node;
using CommonEdge = Common.Domain.Models.NewGraph.Edge;
using CommonStation = Common.Domain.Models.NewGraph.Station;
using CommonRoutingEdge = Common.Domain.Models.NewGraph.RoutingEdge;
using CommonActionRoutingEdge = Common.Domain.Models.NewGraph.ActionRoutingEdge;

internal static class GraphMapper
{
    public static (IReadOnlyCollection<Node> nodes, IReadOnlyCollection<Edge> edges, IReadOnlyCollection<Station> stations) Convert(CommonGraph graph, IReadOnlyDictionary<Guid, AgvType> agvTypes)
    {
        var loadStates = agvTypes.Values.SelectMany(a => a.LoadStates).ToDictionary(ls => ls.Id);

        var nodes = MapNodes(graph.Nodes);
        var edges = MapEdges(graph.Edges, nodes);
        var stations = MapStations(graph.Stations, nodes);
        
        var routingNodes = MapRoutingNodes(graph.Nodes, agvTypes, loadStates, nodes);
        MapRoutingEdgesFromNodes(graph.Nodes, agvTypes, nodes, routingNodes);
        MapRoutingEdgesFromEdges(graph.Edges, agvTypes, edges, routingNodes);

        return (nodes.Values, edges.Values, stations);
    }

    private static Dictionary<Guid, Node> MapNodes(IReadOnlyCollection<CommonNode> commonNodes)
    {
        var nodes = new Dictionary <Guid, Node>();

        foreach (var commonNode in commonNodes)
        {
            var node = new Node(commonNode.Id, commonNode.Name, commonNode.ShortName,commonNode.LifNodeId, commonNode.X, commonNode.Y, commonNode.LevelId, commonNode.MapId);
            nodes.Add(node.Id, node);
        }

        return nodes;   
    }

    private static Dictionary<Guid, Edge> MapEdges(IReadOnlyCollection<CommonEdge> commonEdges, IReadOnlyDictionary<Guid, Node> nodes)
    {
        var edges = new Dictionary<Guid, Edge>();

        foreach (var commonEdge in commonEdges)
        {
            var startNode = nodes[commonEdge.StartNodeId];
            var endNode = nodes[commonEdge.EndNodeId];

            var edge = new Edge(commonEdge.Id, commonEdge.Name, commonEdge.ShortName, commonEdge.LifEdgeId, startNode, endNode);

            startNode.AttachOutgoingEdge(edge);
            endNode.AttachIncomingEdge(edge);

            edges.Add(edge.Id, edge);
        }

        return edges;
    }

    private static List<Station> MapStations(IReadOnlyCollection<CommonStation> commonStations, IReadOnlyDictionary<Guid, Node> nodes)
    {
        var stations = new List<Station>();

        foreach (var commonStation in commonStations)
        {
            var interactionNodes = commonStation.InteractionNodeIds.Select(i => nodes[i]).ToList();

            var station = new Station(commonStation.Id, commonStation.Name, commonStation.ShortName, commonStation.LifStationId, commonStation.StationHeight, commonStation.X, commonStation.Y, commonStation.Theta, interactionNodes);

            stations.Add(station);
        }

        return stations;
    }

    private static Dictionary<Guid, RoutingNode> MapRoutingNodes(IReadOnlyCollection<CommonNode> commonNodes,
        IReadOnlyDictionary<Guid, AgvType> agvTypes,
        IReadOnlyDictionary<Guid, LoadState> loadStates,
        IReadOnlyDictionary<Guid, Node> nodes)
    {
        var routingNodes = new Dictionary<Guid, RoutingNode>();

        foreach (var commonNode in commonNodes)
        {
            var node = nodes[commonNode.Id];

            foreach (var commonRoutingNode in commonNode.RoutingNodes)
            {
                var agvType = agvTypes[commonRoutingNode.AgvTypeId];
                var loadState = loadStates[commonRoutingNode.LoadStateId];

                var routingNode = new RoutingNode(commonRoutingNode.Id, node, agvType, loadState, commonRoutingNode.Theta);

                node.AddRoutingNode(routingNode);
                routingNodes.Add(routingNode.Id, routingNode);
            }
        }

        return routingNodes;
    }

    private static void MapRoutingEdgesFromNodes(IReadOnlyCollection<CommonNode> commonNodes,
        IReadOnlyDictionary<Guid, AgvType> agvTypes,
        IReadOnlyDictionary<Guid, Node> nodes,
        IReadOnlyDictionary<Guid, RoutingNode> routingNodes)
    {
        foreach (var commonNode in commonNodes)
        {
            var node = nodes[commonNode.Id];

            foreach (var commonRoutingEdge in commonNode.RoutingEdges)
            {
                var routingEdge = CreateRoutingEdge(commonRoutingEdge, node, agvTypes, routingNodes);

                node.AddRoutingEdge(routingEdge);
            }
        }
    }

    private static void MapRoutingEdgesFromEdges(IReadOnlyCollection<CommonEdge> commonEdges,
        IReadOnlyDictionary<Guid, AgvType> agvTypes,
        IReadOnlyDictionary<Guid, Edge> edges,
        IReadOnlyDictionary<Guid, RoutingNode> routingNodes)
    {
        foreach (var commonEdge in commonEdges)
        {
            var edge = edges[commonEdge.Id];

            foreach (var commonRoutingEdge in commonEdge.RoutingEdges)
            {
                var routingEdge = CreateRoutingEdge(commonRoutingEdge, edge, agvTypes, routingNodes);

                edge.AddRoutingEdge(routingEdge);
            }
        }
    }

    private static RoutingEdge CreateRoutingEdge(CommonRoutingEdge commonRoutingEdge, GraphResource parent, IReadOnlyDictionary<Guid, AgvType> agvTypes, IReadOnlyDictionary<Guid, RoutingNode> routingNodes)
    {
        var agvType = agvTypes[commonRoutingEdge.AgvTypeId];
        var startNode = routingNodes[commonRoutingEdge.StartNodeId];
        var endNode = routingNodes[commonRoutingEdge.EndNodeId];

        RoutingEdge routingEdge;

        if (commonRoutingEdge is CommonActionRoutingEdge actionRoutingEdge)
        {
            routingEdge = new ActionRoutingEdge(commonRoutingEdge.Id,
                                        parent,
                                        agvType,
                                        startNode,
                                        endNode,
                                        commonRoutingEdge.Length,
                                        commonRoutingEdge.VehicleOrientation,
                                        commonRoutingEdge.OrientationType,
                                        commonRoutingEdge.RotationAtStart,
                                        commonRoutingEdge.RotationAtEnd,
                                        commonRoutingEdge.RotationAllowed,
                                        commonRoutingEdge.MaxSpeed,
                                        commonRoutingEdge.MaxRotationSpeed,
                                        commonRoutingEdge.Trajectory,
                                        actionRoutingEdge.ActionType);
        }
        else
        {
                    routingEdge = new RoutingEdge(commonRoutingEdge.Id,
                                parent,
                                agvType,
                                startNode,
                                endNode,
                                commonRoutingEdge.Length,
                                commonRoutingEdge.VehicleOrientation,
                                commonRoutingEdge.OrientationType,
                                commonRoutingEdge.RotationAtStart,
                                commonRoutingEdge.RotationAtEnd,
                                commonRoutingEdge.RotationAllowed,
                                commonRoutingEdge.MaxSpeed, 
                                commonRoutingEdge.MaxRotationSpeed,
                                commonRoutingEdge.Trajectory);
        }

        startNode.AttachOutgoingEdge(routingEdge);
        endNode.AttachIncomingEdge(routingEdge);

        return routingEdge;
    }
}