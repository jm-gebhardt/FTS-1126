using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.Models.Graphs;
using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;
using JetBrains.Annotations;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

/// <summary>
/// A location of a trigger.
/// </summary>
[PublicAPI]
public class Location : IEquatable<Location>
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
    public GraphNode? Node { get; set; }

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
    public ICollection<ConfigurationParameter> ConfigurationParameters { get; set; }

    /// <summary>
    /// AGV Ids which are currently located within the polygon.
    /// </summary>
    public ICollection<Guid> Agvs { get; set; }

    /// <inheritdoc cref="Location"/>>
    public Location()
    {
        Id = Guid.NewGuid();
        Polygon = [];
        Agvs = [];
        TriggerId = Guid.NewGuid();
        ConfigurationParameters = [];
    }

    /// <inheritdoc cref="Location"/>>
    public Location(GraphNode? node, Guid? edgeId, Guid? mapId, Guid triggerId, ICollection<ConfigurationParameter> configurationParameters)
    {
        Id = Guid.NewGuid();
        Polygon = [];
        Agvs = [];
        Node = node;
        EdgeId = edgeId;
        MapId = mapId ?? null;
        TriggerId = triggerId;
        ConfigurationParameters = configurationParameters.Select(p => new ConfigurationParameter(p.Name, p.Description, p.Value, p.Validation)).ToList();
    }

    /// <inheritdoc cref="Location"/>>
    public Location(ICollection<Polygon.PolygonPoint> polygon, Guid? mapId, Guid triggerId, ICollection<ConfigurationParameter> configurationParameters, ICollection<Guid>? agvs = null)
    {
        Id = Guid.NewGuid();
        Polygon = polygon.Select(p => new TriggerPolygonPoint(p.X, p.Y, p.Index, Id)).ToList();
        Agvs = agvs ?? [];
        MapId = mapId ?? null;
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

    /// <inheritdoc />
    public bool Equals(Location? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (!Id.Equals(other.Id))
        {
            return false;
        }
        if (TriggerId != other.TriggerId)
        {
            return false;
        }
        if (EdgeId != other.EdgeId)
        {
            return false;
        }
        if (Node?.Id != other.Node?.Id)
        {
            return false;
        }
        if (MapId != other.MapId)
        {
            return false;
        }
        if (!Agvs.SequenceEqual(other.Agvs))
        {
            return false;
        }
        if (!Polygon.SequenceEqual(other.Polygon))
        {
            return false;
        }
        if (!ConfigurationParameters.SequenceEqual(other.ConfigurationParameters))
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == GetType() && Equals((Location) obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Id, TriggerId, EdgeId, Node, MapId, Agvs, ConfigurationParameters, Polygon);
    }
}