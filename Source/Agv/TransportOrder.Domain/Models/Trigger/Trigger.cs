using Gebhardt.Acc.Common.Messages.Triggers;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using JetBrains.Annotations;
using System.Text.RegularExpressions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Polygon;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

/// <summary>
/// A trigger
/// </summary>
public class Trigger
{
    /// <inheritdoc cref="Trigger"/>>
    public Trigger(Guid id, string urn, string displayName, string version, string description, int? instanceMaximum, ICollection<ConfigurationParameter> configurationParameters)
    {
        Id = id;
        Urn = urn;
        DisplayName = displayName;
        Version = version;
        Description = description;
        InstanceMaximum = instanceMaximum;
        ConfigurationParameters = configurationParameters;
        Locations = [];
    }

    /// <inheritdoc cref="Trigger"/>>
    [UsedImplicitly]
    private Trigger()
    {
        Id = Guid.Empty;
        ConfigurationParameters = [];
        Urn = string.Empty;
        DisplayName = string.Empty;
        Description = string.Empty;
        Version = string.Empty;
        InstanceMaximum = null;
        Locations = [];
    }

    /// <summary>
    /// The id of the trigger.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The unique resource name : 
    /// </summary>
    public string Urn { get; init; }

    /// <summary>
    /// Trigger name as displayed in the ui.
    /// </summary>
    public string DisplayName { get; init; }

    /// <summary>
    /// Version of the trigger : "0.0.1"
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Description of the triggers functionality
    /// </summary>
    public string Description { get; init; } 

    /// <summary>
    /// If set, the maximum allowed trigger instances in the graph 
    /// </summary>
    public int? InstanceMaximum { get; init; }

    /// <summary>
    /// The configuration parameters of the trigger.
    /// </summary>
    public ICollection<ConfigurationParameter> ConfigurationParameters { get; init; }

    /// <summary>
    /// The locations of the trigger.
    /// </summary>
    public ICollection<Location> Locations { get; init; }

    /// <summary>
    /// Add an edge location.
    /// </summary>
    /// <param name="edgeId">The edge id.</param>
    /// <param name="mapId">Id of the map the edge is on.</param>
    /// <returns>The new location</returns>
    /// <exception cref="TriggerInstanceCountException">Thrown when the instance maximum was exceeded.</exception>
    public Location AddLocation(Guid edgeId, Guid mapId)
    {
        if (InstanceMaximum != null && Locations.Count == InstanceMaximum)
        {
            throw new TriggerInstanceCountException(InstanceMaximum!.Value);
        }
        var location = new Location(edgeId, mapId, Id);
        Locations.Add(location);
        return location ;
    }

    /// <summary>
    /// Add a node location.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <param name="mapId">Id of the map the node is on.</param>
    /// <returns>The new location</returns>
    /// <exception cref="TriggerInstanceCountException">Thrown when the instance maximum was exceeded.</exception>
    public Location AddLocation(string nodeId, Guid mapId)
    {
        if (InstanceMaximum != null && Locations.Count == InstanceMaximum)
        {
            throw new TriggerInstanceCountException(InstanceMaximum!.Value);
        }
        var location = new Location(nodeId, mapId, Id);
        Locations.Add(location);
        return location;
    }

    /// <summary>
    /// Add a polygon location.
    /// </summary>
    /// <param name="polygon">The polygon.</param>
    /// <param name="mapId">Id of the map the polygon is on.</param>
    /// <returns>The new location</returns>
    /// <exception cref="TriggerInstanceCountException">Thrown when the instance maximum was exceeded.</exception>
    public Location AddLocation(ICollection<PolygonPoint> polygon, Guid mapId)
    {
        if (InstanceMaximum != null && Locations.Count == InstanceMaximum)
        {
            throw new TriggerInstanceCountException(InstanceMaximum!.Value);
        }
        var location = new Location(polygon, mapId, Id);
        Locations.Add(location);
        return location;
    }

    /// <summary>
    /// Remove a location.
    /// </summary>
    /// <param name="locationId">The location id.</param>
    /// <exception cref="TriggerLocationNotFoundException">Thrown when the location was not found.</exception>
    public void RemoveLocation(Guid locationId)
    {
        var location = Locations.SingleOrDefault(l => l.Id == locationId);
        if (location == null)
        {
            throw new TriggerLocationNotFoundException(locationId);
        }
        Locations.Remove(location);
    }

    /// <summary>
    /// Update configuration parameter.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="value"></param>
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
    /// Validate the value of a configuration parameter.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The value to validate.</param>
    /// <returns></returns>
    public bool ValidateConfigurationParameterValue(string name, string value)
    {
        var parameter = ConfigurationParameters.SingleOrDefault(p => p.Name == name);
        if (parameter == null)
        {
            return false;
        }
        if (parameter.Validation == string.Empty || !IsValidRegex(parameter.Validation))
        {
            return true;
        }
        return Regex.IsMatch(value, CleanupPattern(parameter.Validation));
    }

    private string CleanupPattern(string pattern)
    {
        pattern = pattern.Trim();
        pattern = pattern.Replace(@"\\", @"\");
        return pattern;
    }

    private bool IsValidRegex(string pattern)
    {
        try
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Regex.Match("", CleanupPattern(pattern));
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }
}