using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class MapGenerator
{
    public static Map Generate(
        Guid correlationId = default,
        byte[]? image = null,
        string mapName = "map",
        int width = 10,
        int height = 20,
        double resolution = 2,
        double xOffset = 0,
        double yOffset = 0,
        DateTime modificationDate = default,
        ICollection<GraphNode>? nodes = null,
        ICollection<GraphEdge>? edges = null,
        ICollection<MapAgv>? mapAgvs = null,
        byte[]? customImage = null,
        bool withCustomImage = false
    )
    {
        if (correlationId == Guid.Empty)
        {
            correlationId = Guid.NewGuid();
        }
        return new Map(correlationId,
            mapName,
            MapBackgroundGenerator.Generate(image, correlationId, width, height, resolution, xOffset, yOffset),
            modificationDate)
        {
            Nodes = nodes ?? new List<GraphNode>(),
            Edges = edges ?? new List<GraphEdge>(),
            MapAgvs = mapAgvs ?? new List<MapAgv>(),
            CustomBackground = withCustomImage ? MapBackgroundGenerator.Generate(customImage, correlationId, width, height, resolution, xOffset, yOffset) : null
        };
    }

    public static Map Generate(
        Guid correlationId,
        MapBackground? background = null,
        string mapName = "map",
        DateTime modificationDate = default
    )
    {
        return new Map(correlationId, mapName, background, modificationDate)
        {
            Nodes = new List<GraphNode>(),
            Edges = new List<GraphEdge>(),
            MapAgvs = new List<MapAgv>()
        };
    }
}