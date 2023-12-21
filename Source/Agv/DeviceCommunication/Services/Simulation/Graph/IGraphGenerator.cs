using Gebhardt.StoreWare.Agv.Common.Models.Graphs;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.DeviceCommunication.Services.Simulation.Graph;

/// <summary>
/// Graph Generator
/// </summary>
[PublicAPI]
public interface IGraphGenerator
{
    /// <summary>
    /// List of source nodes
    /// </summary>
    string[] Sources { get; }

    /// <summary>
    /// List of destination nodes
    /// </summary>
    string[] Destinations { get; }

    /// <summary>
    /// Generate Agv Graph
    /// </summary>
    /// <returns></returns>
    public AgvGraph Generate(Guid correlationId);

    /// <summary>
    /// Generate Graph.
    /// </summary>
    /// <returns></returns>
    Common.Models.Graphs.Graph GenerateGraph();

    /// <summary>
    /// Generate Maps.
    /// </summary>
    /// <returns></returns>
    IEnumerable<Common.Models.Graphs.Map> GenerateMaps();
}