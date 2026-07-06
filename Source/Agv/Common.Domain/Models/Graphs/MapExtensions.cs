using Gebhardt.Acc.Common.Domain.Services;

namespace Gebhardt.Acc.Common.Domain.Models.Graphs;

/// <summary>
/// Provides extension methods for maps.
/// </summary>
public static class MapExtensions
{
    private const float MarginMeters = 2.0f;
    private const float MarginFraction = 0.1f;

    extension(Map map)
    {
        /// <summary>
        /// Get map bounds in world coordinates.
        /// </summary>
        /// <param name="resolution"></param>
        /// <returns></returns>
        public (float minX, float maxX, float minY, float maxY) GetMapBoundaries(float resolution)
        {
            // 
            // XOffset = distance from left edge to origin → left boundary = XOffset
            // YOffset = distance from top edge to origin  → top  boundary = YOffset
            float minX = map.XOffset;
            float maxX = map.Width * resolution + map.XOffset;
            float minY = map.YOffset;
            float maxY = map.Height * resolution + map.YOffset;
            return (minX, maxX, minY, maxY);
        }

        /// <summary>
        /// Extends a map to cover nodes from <paramref name="graph"/> that fall outside the existing bounds.
        /// Parameters (width, height, offsets, image) are recomputed; resolution and identity are preserved.
        /// </summary>
        /// <param name="graph">New graph whose nodes may extend beyond the current map area.</param>
        /// <param name="timeProvider">Time provider.</param>
        /// <param name="extraBounds">Additional boundaries for map extension (e.g. for larger images).</param>
        /// <param name="outputResolution">Final resolution.</param>
        /// <returns>A new map with the combined bounds and merged nodes/edges.</returns>
        public Map Extend(AgvGraph graph, TimeProvider timeProvider,
            (float MinX, float MinY, float MaxX, float MaxY)? extraBounds = null,
            float? outputResolution = null)
        {
            float resolution = map.Resolution;
            float outResolution = outputResolution ?? resolution;

            (float minX, float maxX, float minY, float maxY) = map.GetMapBoundaries(resolution);

            // Expand bounds to cover any new nodes that fall outside the existing map
            foreach (var node in graph.Nodes)
            {
                if (node.X < minX)
                {
                    minX = node.X;
                }
                if (node.X > maxX)
                {
                    maxX = node.X;
                }
                if (node.Y < minY)
                {
                    minY = node.Y;
                }
                if (node.Y > maxY)
                {
                    maxY = node.Y;
                }
            }

            // Expand bounds to cover any explicitly provided world bounds (e.g. incoming background image),
            // so margins are computed around the union of node bounds and image bounds.
            if (extraBounds is { } eb)
            {
                minX = Math.Min(minX, eb.MinX);
                maxX = Math.Max(maxX, eb.MaxX);
                minY = Math.Min(minY, eb.MinY);
                maxY = Math.Max(maxY, eb.MaxY);
            }

            float rangeX = maxX - minX;
            float rangeY = maxY - minY;

            float marginX = Math.Max(MarginMeters, rangeX * MarginFraction);
            float marginY = Math.Max(MarginMeters, rangeY * MarginFraction);
            
            if (extraBounds != null)
            {
                marginX = 0;
                marginY = 0;
            }

            float totalX = rangeX + 2 * marginX;
            float totalY = rangeY + 2 * marginY;

            float newXOffset = minX - marginX;
            float newYOffset = minY - marginY;
            int newWidth = (int)Math.Ceiling(totalX / outResolution);
            int newHeight = (int)Math.Ceiling(totalY / outResolution);

            return new Map(
                correlationId: map.CorrelationId,
                mapId: map.MapId,
                modificationDate: timeProvider.GetUtcNow().UtcDateTime,
                imageData: MapHelper.GenerateWhitePngBase64(newWidth, newHeight),
                width: newWidth,
                height: newHeight,
                resolution: outResolution,
                xOffset: newXOffset,
                yOffset: newYOffset,
                nodes: graph.Nodes,
                edges: graph.Edges);
        }
    }
}
