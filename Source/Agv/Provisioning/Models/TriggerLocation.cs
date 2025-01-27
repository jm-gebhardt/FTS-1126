using JetBrains.Annotations;
using Gebhardt.Acc.Common.Domain.Utilities;
using Gebhardt.Acc.Common.Messages.Triggers;

namespace Gebhardt.Acc.Provisioning.Models;

/// <summary>
/// Base data of the trigger location.
/// </summary>
[UsedImplicitly]
internal class TriggerLocation
{
    /// <summary>
    /// The id of the trigger.
    /// </summary>
    [UsedImplicitly]
    public Guid TriggerId
    {
        get;
        init;
    }

    /// <summary>
    /// The urn of the trigger.
    /// </summary>
    public string TriggerUrn
    {
        get;
        [UsedImplicitly]
        init;
    } = null!;

    /// <summary>
    /// The version of the trigger.
    /// </summary>
    public string TriggerVersion
    {
        get;
        [UsedImplicitly]
        init;
    } = null!;

    /// <summary>
    /// The node id / Name.
    /// </summary>
    public string? NodeId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <summary>
    /// The edge id.
    /// </summary>
    public Guid? EdgeId
    {
        get;
        [UsedImplicitly]
        init;
    } 

    /// <summary>
    /// The polygon, if set.
    /// </summary>
    [UsedImplicitly]
    public ICollection<PolygonPoint>? Polygon { get; init; } = [];

    /// <summary>
    /// The configuration parameters at the location, if set.
    /// </summary>
    [UsedImplicitly]
    public ICollection<ConfigurationParameter>? ConfigurationParameters { get; init; } = [];

    /// <summary>
    /// The correlation id of the map.
    /// </summary>
    public Guid? MapId
    {
        get;
        [UsedImplicitly]
        init;
    }

    /// <inheritdoc cref="TriggerLocation"/>
    public TriggerLocation()
    {
        TriggerId = $"{TriggerUrn}:{TriggerVersion}".ToGuid();
    }

    /// <inheritdoc cref="TriggerLocation"/>
    public TriggerLocation(string triggerUrn, string triggerVersion, string? nodeId, Guid? edgeId, List<PolygonPoint>? polygonPoints, Guid? mapId, List<ConfigurationParameter>? configurationParameters)
    {
        TriggerUrn = triggerUrn;
        TriggerVersion = triggerVersion;
        TriggerId = $"{TriggerUrn}:{TriggerVersion}".ToGuid();
        NodeId = nodeId;
        EdgeId = edgeId;
        MapId = mapId;
        Polygon = polygonPoints;
        ConfigurationParameters=configurationParameters;
    }
}