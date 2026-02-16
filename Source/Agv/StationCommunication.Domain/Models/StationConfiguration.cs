using JetBrains.Annotations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gebhardt.Acc.StationCommunication.Domain.Models;

/// <summary>
/// Provides the configuration of the stations
/// </summary>
[PublicAPI]
public class StationConfiguration
{
    /// <summary>
    /// Stations.
    /// </summary>
    public ICollection<Station> Stations { get; set; }

    /// <summary>
    /// The path to the configuration file.
    /// </summary>
    public string? ConfigFilePath { get; set; }

    /// <inheritdoc cref="StationConfiguration"/>
    public StationConfiguration()
    {
        Stations = [];
    }

    /// <summary>
    /// Reload the configuration from file.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no configuration path is set.</exception>
    public void Reload()
    {
        if (string.IsNullOrEmpty(ConfigFilePath))
        {
            throw new InvalidOperationException("Cannot reload configuration: No configuration file path is set.");
        }

        using var reader = new StreamReader(ConfigFilePath);
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        var yamlData = deserializer.Deserialize<StationConfiguration>(reader);
        Stations = yamlData.Stations;
    }
}