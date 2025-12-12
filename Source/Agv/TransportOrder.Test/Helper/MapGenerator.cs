using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.TransportOrder.Domain.Models.Map;
using Map = Gebhardt.Acc.TransportOrder.Domain.Models.Map.Map;

namespace Gebhardt.Acc.TransportOrder.Test.Helper;

public static class MapGenerator
{
    public static Map Generate(
        Guid correlationId = default,
        byte[]? image = null,
        string mapId = "map",
        int width = 10,
        int height = 20,
        double resolution = 2,
        double xOffset = 0,
        double yOffset = 0,
        DateTime modificationDate = default,
        ICollection<GraphNode>? nodes = null,
        ICollection<GraphEdge>? edges = null,
        ICollection<MapAgv>? mapAgvs = null
    )
    {
        if (correlationId == Guid.Empty)
        {
            correlationId = Guid.NewGuid();
        }
        return new Map(correlationId,
            mapId,
            MapBackgroundGenerator.Generate(image, correlationId, width, height, resolution, xOffset, yOffset),
            modificationDate)
        {
            Nodes = nodes ?? new List<GraphNode>(),
            Edges = edges ?? new List<GraphEdge>(),
            MapAgvs = mapAgvs ?? new List<MapAgv>()
        };
    }

    public static Map Generate(
        Guid correlationId,
        MapBackground? background = null,
        string mapId = "map",
        DateTime modificationDate = default
    )
    {
        return new Map(correlationId, mapId, background, modificationDate)
        {
            Nodes = new List<GraphNode>(),
            Edges = new List<GraphEdge>(),
            MapAgvs = new List<MapAgv>()
        };
    }
}