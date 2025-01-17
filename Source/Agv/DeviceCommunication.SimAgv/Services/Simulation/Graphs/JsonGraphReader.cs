using Gebhardt.Acc.DeviceCommunication.SimAgv.Configuration;
using System.Text.Json;

namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <inheritdoc cref="IGraphReader"/>>
public class JsonGraphReader(KarisExportConfiguration configuration) : IGraphReader
{
    /// <inheritdoc cref="IGraphReader"/>>
    public ExportedGraph Read()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        string path = Path.Combine(AppContext.BaseDirectory, configuration.ExportDirectory!, "Exported.json");
        if (!Path.Exists(path))
        {
            throw new Exception($"No graph configuration '{path}' found");
        }
        string json = File.ReadAllText(path);
        var exportedGraph = JsonSerializer.Deserialize<ExportedGraph>(json, options);
        if (exportedGraph == null)
        {
            throw new Exception($"Graph could not be deserialized from '{path}'");
        }

        return exportedGraph;
    }
}