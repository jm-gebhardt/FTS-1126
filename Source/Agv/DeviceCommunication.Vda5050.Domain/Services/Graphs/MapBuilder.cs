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
    /// <param name="mapId">The map Id.</param>
    /// <param name="graph">The graph (source of filtered nodes/edges).</param>
    /// <param name="timeProvider">Time provider.</param>
    /// <param name="minX"></param>
    /// <param name="maxX"></param>
    /// <param name="minY"></param>
    /// <param name="maxY"></param>
    /// <returns>One map per layout (MapId group).</returns>
    public static Map BuildDummyMap(string mapId, AgvGraph graph, TimeProvider timeProvider, float? minX = null, float? maxX = null, float? minY = null, float? maxY = null)
    {
        (int width, int height, float resolution, float xOffset, float yOffset) = MapHelper.ComputeMapGeometry(graph.Nodes.ToList());

        return new Map(
            correlationId: mapId.ToGuid(),
            mapId: mapId,
            modificationDate: timeProvider.GetUtcNow().UtcDateTime,
            imageData: MapHelper.GenerateWhitePngBase64(width, height),
            width: width,
            height: height,
            resolution: resolution,
            xOffset: xOffset,
            yOffset: yOffset,
            nodes: graph.Nodes.ToList(),
            edges: graph.Edges);
    }
}