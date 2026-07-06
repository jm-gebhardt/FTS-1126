using Gebhardt.Acc.Common.Domain.Models.Graphs;

namespace Gebhardt.Acc.Common.Domain.Services;

/// <summary>
/// Base class for map generators.
/// </summary>
public abstract class MapGeneratorBase : IMapGenerator
{
    /// <inheritdoc />
    public abstract bool CanBeUsed(string mapName);

    /// <summary>
    /// Override this method to generate a map.
    /// </summary>
    /// <param name="mapId">The unique Id of the map.</param>
    /// <param name="mapName">The name of the map.</param>
    /// <param name="graph">The graph which is included in the map.</param>
    /// <param name="min">Lowest x/y corner of the graph in world coordinates.</param>
    /// <param name="max">Highest x/y corner of the graph in world coordinates.</param>
    /// <returns></returns>
    protected abstract Map GenerateInternal(Guid mapId, string mapName, IGraph graph, (float X, float Y) min, (float X, float Y) max);

    /// <inheritdoc />
    public virtual Map Generate(Guid mapId, string mapName, IGraph graph)
    {
        if (graph.Nodes.Count == 0)
        {
            throw new InvalidOperationException("Failed to create map due to empty node list.");
        }

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (var node in graph.Nodes)
        {
            if (node.X < minX)
            {
                minX = node.X;
            }
            if (node.Y < minY)
            {
                minY = node.Y;
            }
            if (node.X > maxX)
            {
                maxX = node.X;
            }
            if (node.Y > maxY)
            {
                maxY = node.Y;
            }
        }

        return GenerateInternal(mapId, mapName, graph, (minX, minY), (maxX, maxY));
    }
}