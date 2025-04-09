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
    public string[] PrismatKnownTargets { get; } = ["1020", "1040", "1050", "1060", "2130", "2110", "2230", "2210", "2330", "2310", "2430", "2410"];

    /// <summary>
    /// The source at APL as identified by Prismat.
    /// </summary>
    public string PrismatAplSource = "9060";

    /// <summary>
    /// The target at APL as identified by Prismat.
    /// </summary>
    public string PrismatAplTarget = "9070";

    /// <summary>
    /// The input lanes sent by prismat that is to be handled with an immediate pick after dropping.
    /// </summary>
    public List<string> PrismatInputLanes { get; private set; } = ["1020", "1040"];

    /// <summary>
    /// The output lanes sent by prismat .
    /// </summary>
    public List<string> PrismatOutputLanes { get; private set; } = ["1050", "1060"];

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
    /// The name of the trigger where Doppelspiel is initiated.
    /// </summary>
    public string DoppelspielTriggerName { get; private set; } = "DP3";

    /// <summary>
    /// The name of the node which is used as the move target in the long loop.
    /// to conveyor management.
    /// </summary>
    public string LongLoopMoveTargetNode { get; private set; } = "";

    /// <summary>
    /// Provides a mapping from station ids to naming in the conveyor management.
    /// </summary>
    public Dictionary<string, string> ConveyorStationMapping { get; private set; }

#pragma warning disable CS1591
    public const int ErrorCodeSourceUnknown = 1000;
    public const int ErrorCodeTargetUnknown = 2000;
    public const int ErrorCodeNoRoute = 3000;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    private int[] KnownValidationErrors => [ErrorCodeSourceUnknown, ErrorCodeTargetUnknown, ErrorCodeNoRoute];

    /// <summary>
    /// Errors for host order validation sent back to Prismat
    /// </summary>
    public Dictionary<int, string> OrderValidationErrors { get; private set; } =
        new()
        {
            {ErrorCodeSourceUnknown, "Quell-Station nicht bekannt"},
            {ErrorCodeTargetUnknown, "Ziel-Station nicht bekannt"},
            {ErrorCodeNoRoute, "Route von Quelle zu Ziel nicht bekannt"}
        };

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
    /// Pick targets by drop targets for Doppelspiel (bijective)
    /// </summary>
    public Dictionary<string, string> DoppelspielPickTargets { get; private set; } =
        new()
        {
            {"1050", "1020"},
            {"1060", "1040"},
            {"2110", "2130"},
            {"2210", "2230"},
            {"2310", "2330"},
            {"2410", "2430"},
            {"9070", "9060"}
        };

    /// <summary>
    /// Get the drop target corresponding to the given pick target.
    /// </summary>
    /// <param name="pickTarget"></param>
    /// <returns></returns>
    public string GetDoppelspielDropTargetForPick(string pickTarget)
    {
        foreach ((string drop, string pick) in DoppelspielPickTargets)
        {
            if (pick == pickTarget)
            {
                return drop;
            }
        }
        return string.Empty;
    }

    /// <summary>
    /// Provides a mapping of trigger points to nodes.
    /// </summary>
    public IConcurrentDictionaryProvider<string, string> TriggerPointMapping { get; }

    /// <summary>
    /// True, if the queue at APL is occupied by agvs.
    /// </summary>
    public bool QueueIsOccupied { get; private set; } = true;

    /// <inheritdoc cref="KaeserGraphConfiguration"/>>
    public KaeserGraphConfiguration(IConcurrentDictionaryProvider<string, string> triggerPointMapping)
    {
        TriggerPointMapping = triggerPointMapping;

        TriggerPointMapping.Add(CheckCargoTriggerName, "");
        TriggerPointMapping.Add(DecisionPoint1TriggerName, "");
        TriggerPointMapping.Add(DoppelspielTriggerName, "");

        StationMapping = new Dictionary<string, string>()
        {
            { "1020", "3" },
            { "1050", "3" },
            { "1040", "3" },
            { "1060", "3" },
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
    /// Updates the long loop move target node.
    /// </summary>
    /// <param name="targetNodeId">The target node id.</param>
    public void UpdateLongLoopMoveTargetNode(string targetNodeId)
    {
        LongLoopMoveTargetNode = targetNodeId;
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
        DoppelspielTriggerName = yamlData.DoppelspielTriggerName;
        LongLoopMoveTargetNode = yamlData.LongLoopMoveTargetNode;
        if (!string.IsNullOrEmpty(yamlData.LongLoopMoveTargetNode))
        {
            LongLoopMoveTargetNode = yamlData.LongLoopMoveTargetNode;
        }
        StationMapping = yamlData.StationMapping;
        ConveyorStationMapping = yamlData.ConveyorStationMapping;
        if (!yamlData.TargetLiftHeight.IsNullOrEmpty())
        {
            UpdateLiftHeights(yamlData.TargetLiftHeight);
        }

        if (!yamlData.DoppelspielPickTargets.IsNullOrEmpty())
        {
            UpdateDoppelspielPickTargets(yamlData.DoppelspielPickTargets);
        }

        if (!yamlData.OrderValidationErrors.IsNullOrEmpty())
        {
            UpdateOrderValidationErrors(yamlData.OrderValidationErrors);
        }
    }

    /// <summary>
    /// Update order validation errors.
    /// </summary>
    /// <param name="updatedValidationErrors"></param>
    public void UpdateOrderValidationErrors(Dictionary<int, string> updatedValidationErrors)
    {
        foreach ((int errorCode, string errorInformation) in updatedValidationErrors)
        {
            if (IsKnownErrorCode(errorCode))
            {
                OrderValidationErrors[errorCode] = errorInformation;
            }
        }
    }

    private bool IsKnownErrorCode(int errorCode)
    {
        if (!KnownValidationErrors.Contains(errorCode))
        {
            throw new Exception($"Unknown order validation error code {errorCode}, must be on of {string.Join(',', KnownValidationErrors.Select(e => e.ToString()))}");
        }
        return true;
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
    /// Set a new short loop target.
    /// </summary>
    /// <param name="target">The new target station.</param>
    public void SetShortLoopTarget(string target)
    {
        foreach (string pickTarget in PrismatInputLanes)
        {
            StationMapping[pickTarget] = target;
        }
        foreach (string dropTarget in PrismatOutputLanes)
        {
            StationMapping[dropTarget] = target;
        }
    }

    /// <summary>
    /// Get the short loop target station name.
    /// </summary>
    public string GetShortLoopTarget()
    {
        return StationMapping[PrismatInputLanes.First()];
    }

    /// <summary>
    /// Update DoppelSpiel pick targets
    /// </summary>
    /// <param name="updatedPickTargets"></param>
    public void UpdateDoppelspielPickTargets(Dictionary<string, string> updatedPickTargets)
    {
        foreach ((string dropTarget, string pickTarget) in updatedPickTargets)
        {
            DoppelspielPickTargets[dropTarget] = pickTarget;
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

    /// <summary>
    /// Set <see cref="QueueIsOccupied"/>.
    /// </summary>
    /// <param name="isOccupied">The new value.</param>
    public void SetQueueIsOccupied(bool isOccupied)
    {
        QueueIsOccupied = isOccupied;
    }

    /// <summary>
    /// If true, skip Doppelspiel
    /// </summary>
    /// <param name="target">Target station.</param>
    public bool SkipDoppelspiel(string target)
    {
        return target == "9070" && QueueIsOccupied;
    }
}