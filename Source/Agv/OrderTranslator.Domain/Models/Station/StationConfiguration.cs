using JetBrains.Annotations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gebhardt.Acc.OrderTranslator.Domain.Models.Station;

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

    /// <summary>
    /// Get all sources configured for the stations.
    /// </summary>
    /// <returns>A collection of input lanes.</returns>
    public ICollection<string> GetKnownSources()
    {
        return Stations.SelectMany(s => s.InputLanes).Distinct().ToList();
    }

    /// <summary>
    /// Get targets configured for the stations.
    /// </summary>
    /// <returns>A collection of output lanes.</returns>
    public ICollection<string> GetKnownTargets()
    {
        return Stations.SelectMany(s => s.OutputLanes).Distinct().ToList();
    }

    /// <summary>
    /// Get all station labels used (lanes and names).
    /// </summary>
    public HashSet<string> GetLabels()
    {
        var sources = GetKnownSources();
        var targets = GetKnownTargets();
        var names = Stations.Select(s => s.Name).ToList();
        return [..names, ..sources, ..targets];
    }
}