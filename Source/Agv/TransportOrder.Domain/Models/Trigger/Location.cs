using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

/// <summary>
/// A location of a trigger.
/// </summary>
[PublicAPI]
public class Location
{
    /// <summary>
    /// The id of the location.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The trigger the location belongs to.
    /// </summary>
    public Guid TriggerId { get; set; }

    /// <summary>
    /// The edge id, if set.
    /// </summary>
    public Guid? EdgeId { get; set; }

    /// <summary>
    /// The node id, if set.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// The polygon, if set.
    /// </summary>
    public ICollection<TriggerPolygonPoint> Polygon { get; set; }

    /// <summary>
    /// The id of the map the location is on.
    /// </summary>
    public Guid? MapId { get; set; }

    /// <summary>
    /// The configuration parameters of the trigger.
    /// </summary>
    public ICollection<ConfigurationParameter> ConfigurationParameters { get;  set; }

    /// <inheritdoc cref="Location"/>>
    public Location()
    {
        Id = Guid.NewGuid();
        Polygon = [];
        MapId =  Guid.NewGuid();
        TriggerId = Guid.NewGuid();
        ConfigurationParameters = [];
    }

    /// <inheritdoc cref="Location"/>>
    public Location(string nodeId, Guid? mapId, Guid triggerId, ICollection<ConfigurationParameter> configurationParameters )
    {
        Id = Guid.NewGuid();
        Polygon = [];
        NodeId = nodeId;
        TriggerId = triggerId;
        MapId =  mapId ?? null;
        ConfigurationParameters = configurationParameters.Select(p => new ConfigurationParameter(p.Name, p.Description, p.Value, p.Validation)).ToList();
    }

    /// <inheritdoc cref="Location"/>>
    public Location(Guid edgeId, Guid? mapId, Guid triggerId, ICollection<ConfigurationParameter> configurationParameters)
    {
        Id = Guid.NewGuid();
        Polygon = [];
        EdgeId = edgeId;
        MapId =  mapId ?? null;
        TriggerId = triggerId;
        ConfigurationParameters = configurationParameters.Select(p => new ConfigurationParameter(p.Name, p.Description, p.Value, p.Validation)).ToList();
    }

    /// <inheritdoc cref="Location"/>>
    public Location(ICollection<Polygon.PolygonPoint> polygon, Guid? mapId, Guid triggerId, ICollection<ConfigurationParameter> configurationParameters)
    {
        Id = Guid.NewGuid();
        Polygon = polygon.Select(p => new TriggerPolygonPoint(p.X, p.Y, p.Index, Id)).ToList();
        MapId =  mapId ?? null;
        TriggerId = triggerId;
        ConfigurationParameters = configurationParameters.Select(p => new ConfigurationParameter(p.Name, p.Description, p.Value, p.Validation)).ToList();
    }

    /// <summary>
    /// True, if a position is inside a polygon location.
    /// </summary>
    /// <param name="position">The agv position.</param>
    /// <returns>True, if a position is inside a polygon location.</returns>
    public bool IsPositionAtLocation(AgvPosition position)
    {
        return Polygon.PointIsInPolygon(position.X, position.Y);
    }

    /// <summary>
    /// Update a configuration parameter.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The new value of the parameter.</param>
    public ConfigurationParameter? UpdateConfigurationParameter(string name, string value)
    {
        var parameter = ConfigurationParameters.SingleOrDefault(p => p.Name == name);
        if (parameter == null)
        {
            return null;
        }
        parameter.Value = value;
        return parameter;
    }

    /// <summary>
    /// Finds parameter that use the default value and updates them.
    /// </summary>
    /// <param name="name">The name of the parameter</param>
    /// <param name="oldValue">The old default value.</param>
    /// <param name="newValue">The new default value.</param>
    public void TryUpdateDefaultParameter(string name, string oldValue, string newValue)
    {
        var parameter = ConfigurationParameters.SingleOrDefault(p => p.Name == name);
        if (parameter == null || parameter.Value != oldValue)
        {
            return;
        }
        parameter.Value = newValue;
    }
}
