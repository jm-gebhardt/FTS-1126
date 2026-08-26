using Gebhardt.Acc.Common.Domain.Models.NewGraph;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helper;
using JetBrains.Annotations;
using CommonNode = Gebhardt.Acc.Common.Domain.Models.NewGraph.Node;
using CommonEdge = Gebhardt.Acc.Common.Domain.Models.NewGraph.Edge;
using CommonStation = Gebhardt.Acc.Common.Domain.Models.NewGraph.Station;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Lif;

/// <summary>
/// A collection of lif layouts.
/// </summary>
[PublicAPI]
public class LifLayoutCollection
{
    /// <inheritdoc cref="LifLayoutCollection"/>
    public LifLayoutCollection(Layout[] layouts, MetaInformation metaInformation)
    {
        Layouts = layouts;
        MetaInformation = metaInformation;
    }

    /// <summary>
    /// Collection of layouts used in the facility by the driverless transport system. All layouts refer to the same global origin.
    /// </summary>
    public Layout[] Layouts { get; set; }

    /// <summary>
    /// Contains metadata about the project and the LIF file.
    /// </summary>
    public MetaInformation MetaInformation { get; set; }

    /// <summary>
    /// Get vehicle types supported by the layouts.
    /// </summary>
    /// <returns>VehicleTypeIds included in the layouts.</returns>
    public ICollection<string> GetSupportedVehicleTypes()
    {
        return Layouts.SelectMany(l => l.GetSupportedVehicleTypes()).Distinct().ToList();
    }

    /// <summary>
    /// Generates one graph out of the given layouts, keyed by the deterministic resource ids.
    /// </summary>
    /// <param name="layouts">The layouts to generate the graph from.</param>
    public Graph ToGraph(IReadOnlyCollection<Layout> layouts)
    {
        string lifId = MetaInformation.Creator;

        List<CommonNode> nodes = layouts.SelectMany(layout => GetNodes(lifId, layout)).ToList();
        List<CommonEdge> edges = layouts.SelectMany(layout => GetEdges(lifId, layout)).ToList();
        List<CommonStation> stations = layouts.SelectMany(layout => GetStations(lifId, layout)).ToList();

        // TODO: routing objects

        return new Graph(nodes, edges, stations);
    }

    private static List<CommonNode> GetNodes(string lifId, Layout layout)
    {
        List<CommonNode> nodes = [];

        foreach (var node in layout.Nodes)
        {
            var internalId = InternalIdGenerator.GetNodeId(lifId, node.NodeId, out string name);

            nodes.Add(new CommonNode(internalId, name, node.NodeId, node.NodePosition.X, node.NodePosition.Y, layout.LayoutLevelId));
        }

        return nodes;
    }

    private static List<CommonEdge> GetEdges(string lifId, Layout layout)
    {
        List<CommonEdge> edges = [];

        foreach (var edge in layout.Edges)
        {
            var internalId = InternalIdGenerator.GetEdgeId(lifId, edge.EdgeId, out string name);
            var internalStartNodeId = InternalIdGenerator.GetNodeId(lifId, edge.StartNodeId, out _);
            var internalEndNodeId = InternalIdGenerator.GetNodeId(lifId, edge.EndNodeId, out _);

            edges.Add(new CommonEdge(internalId, name, edge.EdgeId, internalStartNodeId, internalEndNodeId));
        }

        return edges;
    }

    private static List<CommonStation> GetStations(string lifId, Layout layout)
    {
        List<CommonStation> stations = [];

        foreach (var station in layout.Stations)
        {
            var internalId = InternalIdGenerator.GetStationId(lifId, station.StationId, out string name);
            List<Guid> internalInteractionNodeIds = station.InteractionNodeIds.Select(id => InternalIdGenerator.GetNodeId(lifId, id, out _)).ToList();

            if (station.StationPosition is not { } stationPosition)
            {
                throw new ArgumentException($"Station {layout.LayoutId} - {station.StationName} has no position");
            }

            var commonStation = new CommonStation(internalId, name, station.StationId, station.StationHeight, stationPosition.X, stationPosition.Y, stationPosition.Theta)
            {
                InteractionNodeIds = internalInteractionNodeIds
            };

            stations.Add(commonStation);
        }

        return stations;
    }
}