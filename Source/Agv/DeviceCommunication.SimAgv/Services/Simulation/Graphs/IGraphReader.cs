namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Services.Simulation.Graphs;

/// <summary>
/// Represents a reader for exported graphs.
/// </summary>
public interface IGraphReader
{
    /// <summary>
    /// Read an exported graph
    /// </summary>
    public ExportedGraph Read();
}