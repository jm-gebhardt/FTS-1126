using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.DeviceCommunication.Common.Services;
using Gebhardt.Acc.DeviceCommunication.SimAgv.Configuration;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Maps;

/// <summary>
/// Reads a PNG image from file and generates a Map if provided.
/// Uses the <see cref="GeneratedMapGenerator"/> as fallback if the file on the disk is not existing.
/// </summary>
public class ExportedGraphMapGenerator(KarisExportConfiguration configuration) : IMapGenerator
{
    /// <inheritdoc />
    public bool CanBeUsed(string mapName) => true;

    /// <inheritdoc />
    public Map Generate(Guid mapId, string mapName, IGraph graph)
    {
        string searchPattern = $"{mapName}*.png";
        string directory = Path.Combine(AppContext.BaseDirectory, configuration.ExportDirectory!);
        string[] files = Directory.GetFiles(directory, searchPattern);
        IMapGenerator generator = new GeneratedMapGenerator();

        if (files.Length != 0)
        {
            generator =  new MapFromFileGenerator(directory);
        }
       
        return generator.Generate(mapId, mapName, graph);
    }
}