using Gebhardt.Acc.Common.Dictionaries;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;

/// <summary>
/// Provides the configuration of the Kaeser graph
/// </summary>
public class KaeserGraphConfiguration
{
    /// <summary>
    /// Prismat targets where nodes are known.
    /// </summary>
    public string[] PrismatKnownTargets { get; } = ["1020", "1040", "2130", "2110", "2230", "2210", "2330", "2310", "2430", "2410"];

    /// <summary>
    /// Prismat targets for which the pick node is unknown;
    /// </summary>
    public string[] PrismatIncompletePickTargets { get; } = ["1040", "9070"];

    /// <summary>
    /// Prismat targets for which the drop node is unknown;
    /// </summary>
    public string[] PrismatIncompleteDropTargets { get; } = ["9060", "1060"];

    /// <summary>
    /// Provides a mapping between source and target definitions and
    /// station node names in the Kaeser graph.
    /// e.g. 
    /// </summary>
    public Dictionary<string, string> StationMapping { get; private set; }

    /// <summary>
    /// The name of the trigger at decision point in the Kaeser project.
    /// </summary>
    public string DecisionPointName { get; private set; } = "DP2";

    /// <summary>
    /// The name of the trigger where agvs announce their cargo state
    /// to conveyor management.
    /// </summary>
    public string LoadTriggerPointName { get; private set; } = "DP1";

    /// <summary>
    /// Provides a mapping of trigger points to nodes.
    /// </summary>
    public IConcurrentDictionaryProvider<string, string> TriggerPointMapping { get; }

    /// <inheritdoc cref="KaeserGraphConfiguration"/>>
    public KaeserGraphConfiguration(IConcurrentDictionaryProvider<string, string> triggerPointMapping)
    {
        TriggerPointMapping = triggerPointMapping;
        TriggerPointMapping.Add(LoadTriggerPointName, "");
        TriggerPointMapping.Add(DecisionPointName, "");
        StationMapping = new Dictionary<string, string>()
        {
            { "1020", "3" },
            { "1050", "3" },
            { "9060", "4" },
            { "9070", "4" },
            { "2130", "5" },
            { "2110", "5" },
            { "2230", "6" },
            { "2210", "6" },
            { "2330", "7" },
            { "2310", "7" },
            { "2430", "8" },
            { "2410", "8" }
        };
    }

    /// <summary>
    /// Ctor for Yaml
    /// </summary>
    public KaeserGraphConfiguration()
    {
        StationMapping = new Dictionary<string, string>();
        TriggerPointMapping = new InMemoryTriggerPointMapping();
    }

    /// <summary>
    /// Updates the node id of a trigger point.
    /// </summary>
    /// <param name="triggerPointName">The name of the trigger point.</param>
    /// <param name="nodeId">The node id.</param>
    public void UpdateTriggerPointNodeId(string triggerPointName, string nodeId)
    {
        TriggerPointMapping.Add(triggerPointName, nodeId);
    }

    /// <summary>
    /// Update the configuration from a yaml file.
    /// </summary>
    /// <param name="configPath"></param>
    public void UpdateFromFile(string configPath)
    {
        using var reader = new StreamReader(configPath);
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        var yamlData = deserializer.Deserialize<KaeserGraphConfiguration>(reader);

        DecisionPointName = yamlData.DecisionPointName;
        LoadTriggerPointName = yamlData.LoadTriggerPointName;
        StationMapping = yamlData.StationMapping;
    }
}