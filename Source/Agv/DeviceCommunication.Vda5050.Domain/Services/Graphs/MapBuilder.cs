using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Helpers;
using JetBrains.Annotations;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Services.Graphs;

/// <summary>
/// Builds <see cref="Map"/> instances with dynamically sized background images from domain graph data.
/// </summary>
[PublicAPI]
public static class MapBuilder
{
    /// <summary>
    /// Build one <see cref="Map"/> per MapId group from domain graph data.
    /// </summary>
    /// <param name="domainGraph">The VDA5050 domain graph (source of MapId grouping).</param>
    /// <param name="commonGraph">The common AGV graph (source of filtered nodes/edges).</param>
    /// <param name="timeProvider">Time provider.</param>
    /// <returns>One map per layout (MapId group).</returns>
    public static IEnumerable<Map> BuildDummyMaps(
        Models.Graphs.AgvGraph domainGraph,
        AgvGraph commonGraph,
        TimeProvider timeProvider)
    {
        IEnumerable<IGrouping<string, Models.Graphs.Node>> layoutGroups = domainGraph.Nodes
            .Where(n => n.MapId != null)
            .GroupBy(n => n.MapId!);

        foreach (IGrouping<string, Models.Graphs.Node> group in layoutGroups)
        {
            string mapId = group.Key;
            HashSet<Guid> layoutNodeGuids = group.Select(n => n.Id).ToHashSet();
            HashSet<string> layoutNodeIds = group.Select(n => n.NodeId).ToHashSet();

            HashSet<Guid> layoutEdgeGuids = domainGraph.Edges
                .Where(e => layoutNodeIds.Contains(e.StartNodeId) && layoutNodeIds.Contains(e.EndNodeId))
                .Select(e => e.Id)
                .ToHashSet();

            List<GraphNode> mapNodes = commonGraph.Nodes.Where(n => layoutNodeGuids.Contains(n.Id)).ToList();
            List<GraphEdge> mapEdges = commonGraph.Edges.Where(e => layoutEdgeGuids.Contains(e.Id)).ToList();

            (int width, int height, float resolution, float xOffset, float yOffset) = MapHelper.ComputeMapGeometry(mapNodes);

            yield return new Map(
                correlationId: mapId.ToGuid(),
                mapId: mapId,
                modificationDate: timeProvider.GetUtcNow().UtcDateTime,
                imageData: MapHelper.GenerateWhitePngBase64(width, height),
                width: width,
                height: height,
                resolution: resolution,
                xOffset: xOffset,
                yOffset: yOffset,
                nodes: mapNodes,
                edges: mapEdges);
        }
    }
}