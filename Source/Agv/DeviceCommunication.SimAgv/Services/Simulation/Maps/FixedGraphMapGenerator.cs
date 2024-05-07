using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Common.Services;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Maps;

/// <summary>
/// Reads a PNG image from file and generates a Map.
/// Uses the <see cref="GeneratedMapGenerator"/> as fallback if the file on the disk is not existing.
/// </summary>
public class FixedGraphMapGenerator : IMapGenerator
{
    /// <inheritdoc />
    public bool CanBeUsed(string mapName) => true;

    /// <inheritdoc />
    public Map Generate(Guid mapId, string mapName, IGraph graph)
    {
        IMapGenerator generator = new MapFromFileGenerator(Path.Combine(AppContext.BaseDirectory, "Resources", "BackgroundImages"));
        if (!generator.CanBeUsed(mapName))
        {
            generator = new GeneratedMapGenerator();
        }
        return generator.Generate(mapId, mapName, graph);
    }
}