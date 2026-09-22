using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

/// <summary>
/// Represents a resource in the graph.
/// </summary>
[PublicAPI]
public abstract class GraphResource
{
    private readonly List<RoutingResource> _children = [];

    /// <summary>
    /// Unique identifier for the graph resource.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Name of the graph resource.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Description of the graph resource.
    /// </summary>
    public string ShortName { get; }

    /// <inheritdoc cref="GraphResource"/>
    protected GraphResource(Guid id, string name, string shortName)
    {
        Id = id;
        Name = name;
        ShortName = shortName;
    }
}