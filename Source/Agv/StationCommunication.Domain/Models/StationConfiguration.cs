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
    /// True, if the configuration has been loaded from file.
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// Stations.
    /// </summary>
    public ICollection<Station> Stations { get; set; }

    /// <inheritdoc cref="StationConfiguration"/>
    public StationConfiguration()
    {
        IsLoaded = false;
        Stations = [];
    }

    /// <summary>
    /// Update the configuration from a yaml file.
    /// </summary>
    public void UpdateFromFile(string configPath)
    {
        using var reader = new StreamReader(configPath);
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        var yamlData = deserializer.Deserialize<StationConfiguration>(reader);
        Stations = yamlData.Stations;
        IsLoaded = true;
    }
}