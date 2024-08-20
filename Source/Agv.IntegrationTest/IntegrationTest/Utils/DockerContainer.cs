namespace Gebhardt.Acc.IntegrationTest.Utils;

/// <summary>
/// Docker Container model
/// </summary>
public class DockerContainer
{
    /// <summary>
    /// Name of docker container
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// State of docker container
    /// </summary>
    public string State { get; set; } = string.Empty;
}