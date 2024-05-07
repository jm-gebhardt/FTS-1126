namespace Gebhardt.Acc.DeviceCommunication.SimAgv.Exceptions;

/// <summary>
/// Exception thrown when the graph generator was not configured correctly.
/// </summary>
public class GraphGeneratorConfigurationException : Exception
{
    /// <inheritdoc cref="GraphGeneratorConfigurationException"/>>
    public GraphGeneratorConfigurationException(string error) : base(error)
    {
    }
}