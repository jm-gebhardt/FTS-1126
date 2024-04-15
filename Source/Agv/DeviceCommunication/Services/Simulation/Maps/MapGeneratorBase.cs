using Gebhardt.StoreWare.Agv.Common.Domain.Models.Graphs;
using SkiaSharp;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Maps;

/// <summary>
/// Base class for map generators.
/// </summary>
public abstract class MapGeneratorBase : IMapGenerator
{
    /// <summary>
    /// Corner point of the graph with the lowest x and y value.
    /// </summary>
    protected SKPoint Min { get; private set; }

    /// <summary>
    /// Corner point of the graph with the highest x and y value.
    /// </summary>
    protected SKPoint Max { get; private set; }

    /// <inheritdoc />
    public abstract bool CanBeUsed(string mapName);

    /// <summary>
    /// Override this method to generate a map.
    /// </summary>
    /// <param name="mapId">The unique Id of the map.</param>
    /// <param name="mapName">The name of the map.</param>
    /// <param name="graph">The graph which is included in the map.</param>
    /// <returns></returns>
    protected abstract Map GenerateInternal(Guid mapId, string mapName, IGraph graph);

    /// <inheritdoc />
    public virtual Map Generate(Guid mapId, string mapName, IGraph graph)
    {
        if (graph.Nodes.Count == 0)
        {
            throw new InvalidOperationException("Failed to create simulation map due to empty node list.");
        }

        var min = new SKPoint(float.MaxValue, float.MaxValue);
        var max = new SKPoint(float.MinValue, float.MinValue);

        // Iterate once for better performance.
        foreach (var node in graph.Nodes)
        {
            if (node.X < min.X)
            {
                min.X = node.X;
            }
            if (node.Y < min.Y)
            {
                min.Y = node.Y;
            }
            if (node.X > max.X)
            {
                max.X = node.X;
            }
            if (node.Y > max.Y)
            {
                max.Y = node.Y;
            }
        }

        Min = min;
        Max = max;

        return GenerateInternal(mapId, mapName, graph);
    }
}