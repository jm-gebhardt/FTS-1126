using Gebhardt.Acc.Common.Dictionaries;
using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Models.KaeserGraph;

/// <summary>
/// Provides the configuration of the Kaeser graph
/// </summary>
[PublicAPI]
public class KaeserGraphConfiguration
{
    /// <summary>
    /// Prismat targets where nodes are known.
    /// </summary>
    public string[] PrismatKnownTargets { get; } = ["1020", "1050", "2130", "2110", "2230", "2210", "2330", "2310", "2430", "2410"];

    /// <summary>
    /// Prismat targets (pick and drop) of orders for which the pick node is unknown.
    /// </summary>
    public string[] PrismatIncompletePickTargets { get; } = ["1040", "9070"];

    /// <summary>
    /// Prismat targets (pick and drop) of orders for which the drop node is unknown.
    /// </summary>
    public string[] PrismatIncompleteDropTargets { get; } = ["9060", "1060"];

    /// <summary>
    /// The AKL pick target sent by prismat that is to be handled with an immediate pick after dropping.
    /// </summary>
    public string PrismatAklPickTarget { get; private set; } = "1020";

    /// <summary>
    /// Provides a mapping between source and target definitions and
    /// station node names in the Kaeser graph.
    /// e.g. 
    /// </summary>
    public Dictionary<string, string> StationMapping { get; private set; }

    /// <summary>
    /// The name of the trigger at decision point in the Kaeser project.
    /// </summary>
    public string DecisionPoint1TriggerName { get; private set; } = "DP1";

    /// <summary>
    /// The name of the trigger where agvs announce their cargo state
    /// to conveyor management.
    /// </summary>
    public string CheckCargoTriggerName { get; private set; } = "DP2";

    /// <summary>
    /// The name of the trigger where agvs on short loop announce their cargo state
    /// to conveyor management.
    /// </summary>
    public string ShortLoopTriggerName { get; private set; } = "DP3";

    /// <summary>
    /// Provides a mapping from station ids to naming in the conveyor management.
    /// </summary>
    public Dictionary<string, string> ConveyorStationMapping { get; private set; }

    /// <summary>
    /// Provides a mapping of pick and drop lift height at stations 5-8.
    /// </summary>
    public Dictionary<string, double> TargetLiftHeight { get; private set; } =
        new()
        {
            { "2110", 2.211d },
            { "2130", 1.213d },
            { "2210", 2.221d },
            { "2230", 1.223d },
            { "2310", 2.231d },
            { "2330", 1.233d },
            { "2410", 2.241d },
            { "2430", 1.243d }
        };

    /// <summary>
    /// Provides a mapping of trigger points to nodes.
    /// </summary>
    public IConcurrentDictionaryProvider<string, string> TriggerPointMapping { get; }

    /// <inheritdoc cref="KaeserGraphConfiguration"/>>
    public KaeserGraphConfiguration(IConcurrentDictionaryProvider<string, string> triggerPointMapping)
    {
        TriggerPointMapping = triggerPointMapping;

        TriggerPointMapping.Add(CheckCargoTriggerName, "");
        TriggerPointMapping.Add(DecisionPoint1TriggerName, "");
        TriggerPointMapping.Add(ShortLoopTriggerName, "");

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

        ConveyorStationMapping = new Dictionary<string, string>()
        {
            { "1", "WE01" },
            { "2", "WE02" },
            { "3", "WE03" }
        };
    }

    /// <summary>
    /// Ctor for Yaml
    /// </summary>
    public KaeserGraphConfiguration()
    {
        StationMapping = new Dictionary<string, string>();
        ConveyorStationMapping = new Dictionary<string, string>();
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

        DecisionPoint1TriggerName = yamlData.DecisionPoint1TriggerName;
        CheckCargoTriggerName = yamlData.CheckCargoTriggerName;
        ShortLoopTriggerName = yamlData.ShortLoopTriggerName;
        StationMapping = yamlData.StationMapping;
        ConveyorStationMapping = yamlData.ConveyorStationMapping;
        if (!string.IsNullOrEmpty(yamlData.PrismatAklPickTarget))
        {
            PrismatAklPickTarget = yamlData.PrismatAklPickTarget;
        }
        if (!yamlData.TargetLiftHeight.IsNullOrEmpty())
        {
            UpdateLiftHeights(yamlData.TargetLiftHeight);
        }
    }

    /// <summary>
    /// Update lift heights.
    /// </summary>
    /// <param name="updatedLiftHeights">The updated lift heights.</param>
    public void UpdateLiftHeights(Dictionary<string, double> updatedLiftHeights)
    {
        foreach ((string station, double liftHeight) in updatedLiftHeights)
        {
            TargetLiftHeight[station] = liftHeight;
        }
    }

    /// <summary>
    /// Get the lift height for target station.
    /// </summary>
    /// <param name="target">Target station for pick or drop.</param>
    public double? GetLiftHeight(string target)
    {
        return TargetLiftHeight.TryGetValue(target, out double liftHeight) ? liftHeight : null;
    }
}