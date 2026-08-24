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
        Dictionary<Guid, CommonNode> nodes = [];
        Dictionary<Guid, CommonEdge> edges = [];
        Dictionary<Guid, CommonStation> stations = [];

        string projectId = MetaInformation.ProjectIdentification;

        foreach (var layout in layouts)
        {
            Dictionary<Guid, CommonNode> layoutNodes = layout.Nodes.ToDictionary(
                n => LifHelper.GenerateInternalId(n.NodeId, layout.LayoutId, projectId),
                n => n.ToGraphNode(LifHelper.GenerateInternalId(n.NodeId, layout.LayoutId, projectId), layout.LayoutLevelId)
            );

            foreach (var (nodeId, node) in layoutNodes)
            {
                nodes[nodeId] = node;
            }

            foreach (var edge in layout.Edges)
            {
                var edgeId = LifHelper.GenerateInternalId(edge.EdgeId, layout.LayoutId, projectId);
                edges[edgeId] = edge.ToGraphEdge(edgeId, layout.LayoutLevelId, layoutNodes, layout.LayoutId, projectId);
            }

            foreach (var station in layout.Stations)
            {
                var stationId = LifHelper.GenerateInternalId(station.StationId, layout.LayoutId, projectId);
                stations[stationId] = station.ToGraphStation(stationId, layout.LayoutId, projectId, layoutNodes);
            }
        }
        // TODO: routing objects

        return new Graph(nodes.Values.ToList(), edges.Values.ToList(), stations.Values.ToList());
    }
}