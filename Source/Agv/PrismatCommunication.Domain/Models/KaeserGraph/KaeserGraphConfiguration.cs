using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Extensions;
using JetBrains.Annotations;
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
    /// True, if the configuration has been loaded from file.
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// True, if the agv limit at akl station should be used.
    /// </summary>
    public bool? UseAklAgvLimit { get; set; } = true;

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
    /// The left short loop waiting node.
    /// </summary>
    public string LeftShortLoopWaitingNode = "map/NODE-32";

    /// <summary>
    /// The right short loop waiting node.
    /// </summary>
    public string RightShortLoopWaitingNode = "map/Node-27";

    /// <summary>
    /// The waiting position in the short loop by Target source.
    /// </summary>
    public Dictionary<string, string> WaitingPositionByAklSource = new();

    /// <summary>
    /// The waiting position in the short loop by AKL target.
    /// </summary>
    public Dictionary<string, string> WaitingPositionByAklTarget = new();

    /// <summary>
    /// Errors for host order validation sent back to Prismat
    /// </summary>
    public Dictionary<int, string> OrderValidationErrors { get; private set; } =
        new()
        {
            { ErrorCodeSourceUnknown, "Quell-Station nicht bekannt" },
            { ErrorCodeTargetUnknown, "Ziel-Station nicht bekannt" },
            { ErrorCodeNoRoute, "Route von Quelle zu Ziel nicht bekannt" }
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
    /// Pick targets by drop targets for Doppelspiel
    /// </summary>
    public Dictionary<string, List<string>> DoppelspielPickTargets { get; private set; } =
        new()
        {
            { "1050", ["1040", "1020"] },
            { "1060", ["1020", "1040"] },
            { "9070", ["9060"] }
        };

    /// <summary>
    /// Pick targets by drop targets for Doppelspiel at akl stations.
    /// </summary>
    public Dictionary<string, string> AklDoppelspielPickTargets { get; private set; } =
        new()
        {
            { "2110", "2130" },
            { "2210", "2230" },
            { "2310", "2330" },
            { "2410", "2430" }
        };

    /// <summary>
    /// Station groups.
    /// </summary>
    public Dictionary<string, ICollection<Station>> StationGroups { get; private set; } =
        new()
        {
            { "AP01-AP02", [new Station("2130", "2110"), new Station("2230", "2210")] },
            { "AP03-AP04", [new Station("2330", "2310"), new Station("2430", "2410")] }
        };

    /// <summary>
    /// Get the drop target corresponding to the given pick target.
    /// </summary>
    /// <param name="pickTarget"></param>
    /// <returns></returns>
    public string GetDoppelspielDropTargetForPick(string pickTarget) => DoppelspielPickTargets.FirstOrDefault(kv => kv.Value.Contains(pickTarget)).Key ?? string.Empty;

    /// <summary>
    /// Get the pick target corresponding to the given drop target.
    /// </summary>
    /// <param name="dropTarget"></param>
    /// <returns></returns>
    public string? GetDoppelspielPickTargetForDrop(string dropTarget) => AklDoppelspielPickTargets.GetValueOrDefault(dropTarget);

    /// <summary>
    /// Provides a mapping of trigger points to nodes.
    /// </summary>
    public IConcurrentDictionaryProvider<string, Guid> TriggerPointMapping { get; }

    /// <summary>
    /// True, if the queue at APL is occupied by agvs.
    /// </summary>
    public bool QueueIsOccupied { get; private set; } = true;

    /// <summary>
    /// Name of the apl queue.
    /// </summary>
    public string AplQueueName { get; private set; } = "AplQueue";

    /// <inheritdoc cref="KaeserGraphConfiguration"/>>
    public KaeserGraphConfiguration(IConcurrentDictionaryProvider<string, Guid> triggerPointMapping)
    {
        IsLoaded = false;
        TriggerPointMapping = triggerPointMapping;

        TriggerPointMapping.Add(CheckCargoTriggerName, Guid.Empty);
        TriggerPointMapping.Add(DecisionPoint1TriggerName, Guid.Empty);
        TriggerPointMapping.Add(DoppelspielTriggerName, Guid.Empty);

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
            { "1", "PS01" },
            { "2", "PS02" },
            { "3", "PS03" }
        };

        SetDefaultShortLoopWaitingPositions();
    }

    /// <summary>
    /// Ctor for Yaml
    /// </summary>
    public KaeserGraphConfiguration()
    {
        IsLoaded = false;
        StationMapping = new Dictionary<string, string>();
        ConveyorStationMapping = new Dictionary<string, string>();
        TriggerPointMapping = new InMemoryTriggerPointMapping();
        StationGroups = new Dictionary<string, ICollection<Station>>();
    }

    /// <summary>
    /// Updates the node id of a trigger point.
    /// </summary>
    /// <param name="triggerPointName">The name of the trigger point.</param>
    /// <param name="nodeId">The node id.</param>
    public void UpdateTriggerPointNodeId(string triggerPointName, Guid nodeId)
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

        if (!yamlData.AklDoppelspielPickTargets.IsNullOrEmpty())
        {
            UpdateAklDoppelspielDropTargets(yamlData.AklDoppelspielPickTargets);
        }

        if (!yamlData.OrderValidationErrors.IsNullOrEmpty())
        {
            UpdateOrderValidationErrors(yamlData.OrderValidationErrors);
        }

        if (!yamlData.StationGroups.IsNullOrEmpty())
        {
            UpdateStationGroups(yamlData.StationGroups);
        }

        if (yamlData.UseAklAgvLimit.HasValue)
        {
            UseAklAgvLimit = yamlData.UseAklAgvLimit;
        }

        if (!yamlData.LeftShortLoopWaitingNode.IsNullOrEmpty())
        {
            LeftShortLoopWaitingNode = yamlData.LeftShortLoopWaitingNode;
        }

        if (!yamlData.RightShortLoopWaitingNode.IsNullOrEmpty())
        {
            RightShortLoopWaitingNode = yamlData.RightShortLoopWaitingNode;
        }

        SetDefaultShortLoopWaitingPositions();
        UpdateWaitingPositionByAklSource(yamlData.WaitingPositionByAklSource);
        UpdateWaitingPositionByAklTarget(yamlData.WaitingPositionByAklTarget);

        LocalizeStations();
        IsLoaded = true;
    }

    private void SetDefaultShortLoopWaitingPositions()
    {
        WaitingPositionByAklSource = new Dictionary<string, string>()
        {
            { "2130", LeftShortLoopWaitingNode },
            { "2230", LeftShortLoopWaitingNode },
            { "2330", RightShortLoopWaitingNode },
            { "2430", RightShortLoopWaitingNode }
        };

        WaitingPositionByAklTarget = new Dictionary<string, string>()
        {
            { "2110", RightShortLoopWaitingNode },
            { "2210", RightShortLoopWaitingNode },
            { "2310", RightShortLoopWaitingNode },
            { "2410", RightShortLoopWaitingNode }
        };
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

    /// <summary>
    /// Update the station groups.
    /// </summary>
    /// <param name="updatedStations">The new station groups.</param>
    /// <returns>A collection of stations to remove.</returns>
    public void UpdateStationGroups(Dictionary<string, ICollection<Station>> updatedStations)
    {
        foreach ((string groupName, ICollection<Station> stations) in updatedStations)
        {
            var groupId = groupName.ToGuid();
            if (StationGroups.TryGetValue(groupName, out ICollection<Station>? existingStations))
            {
                foreach (var station in stations)
                {
                    var existingStation = existingStations?.FirstOrDefault(s => s.Source == station.Source);
                    if (existingStation != null)
                    {
                        existingStation.AssignToGroup(groupId);
                        existingStation.Localize(StationMapping[station.Source]);
                        continue;
                    }

                    station.AssignToGroup(groupId);
                    station.Localize(StationMapping[station.Source]);
                    StationGroups[groupName].Add(station);
                }
                continue;
            }
            foreach (var station in stations)
            {
                station.AssignToGroup(groupId);
                station.Localize(StationMapping[station.Source]);
            }
            StationGroups[groupName] = stations;
        }
    }

    private void LocalizeStations()
    {
        foreach ((string groupName, ICollection<Station> stations) in StationGroups)
        {
            var groupId = groupName.ToGuid();
            foreach (var station in stations)
            {
                station.AssignToGroup(groupId);
                if (station.Node == null)
                {
                    station.Localize(StationMapping[station.Source]);
                }
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

    private void UpdateWaitingPositionByAklSource(Dictionary<string, string> updatedWaitingPosition)
    {
        foreach ((string source, string waitingPosition) in updatedWaitingPosition)
        {
            WaitingPositionByAklSource[source] = waitingPosition;
        }
    }

    private void UpdateWaitingPositionByAklTarget(Dictionary<string, string> updatedWaitingPosition)
    {
        foreach ((string target, string waitingPosition) in updatedWaitingPosition)
        {
            WaitingPositionByAklTarget[target] = waitingPosition;
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
    public void UpdateDoppelspielPickTargets(Dictionary<string, List<string>> updatedPickTargets)
    {
        foreach ((string dropTarget, List<string> pickTarget) in updatedPickTargets)
        {
            DoppelspielPickTargets[dropTarget] = pickTarget;
        }
    }

    /// <summary>
    /// Update drop targets for akl short loop Doppelspiel.
    /// </summary>
    /// <param name="updatedDropTargets">The new drop targets.</param>
    public void UpdateAklDoppelspielDropTargets(Dictionary<string, string> updatedDropTargets)
    {
        foreach ((string pickTarget, string dropTarget) in updatedDropTargets)
        {
            AklDoppelspielPickTargets[pickTarget] = dropTarget;
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
    /// Checks if the given target is a drop at AKL
    /// </summary>
    /// <param name="target">Target station.</param>
    /// <returns>True, if the target is an AKL drop station.</returns>
    public bool IsDropAtAkl(string target)
    {
        return AklDoppelspielPickTargets.ContainsKey(target);
    }

    /// <summary>
    /// Checks if the given target is a pick at AKL
    /// </summary>
    /// <param name="target">Target station.</param>
    /// <returns>True, if the target is an AKL pick station.</returns>
    public bool IsPickAtAkl(string target)
    {
        return AklDoppelspielPickTargets.ContainsValue(target);
    }

    /// <summary>
    /// Checks if the given node is limited by agv occupancy.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <returns>True, if the node is occupancy-limited.</returns>
    public bool IsOccupancyLimitedNode(string nodeId)
    {
        return ConveyorStationMapping.ContainsKey(nodeId) || StationMapping.ContainsValue(nodeId);
    }

    /// <summary>
    /// Get the target specification
    /// </summary>
    /// <param name="stationName">The name of the station.</param>
    public ICollection<string> GetPrismatTargetForStationName(string stationName)
    {
        List<string> prismatSourcesAndTargets = StationMapping.Where(x => x.Value == stationName)
            .Select(x => x.Key).ToList();
        if (ConveyorStationMapping.ContainsKey(stationName))
        {
            return PrismatOutputLanes;
        }
        if (prismatSourcesAndTargets.Contains(PrismatAplTarget))
        {
            return [PrismatAplTarget];
        }
        return [];
    }
}