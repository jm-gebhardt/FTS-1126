using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents a resource in the graph.
/// </summary>
[PublicAPI]
public abstract record GraphResource
{
    /// <summary>
    /// Unique identifier for the graph resource.
    /// </summary>
    public Guid Id { get; }  // unique identifier derived from LifId + resourceType + nodeId/edgeId/stationId      e.g.: ToGuid("Karis/Node/N1")  ToGuid("Still/Node/N1")?

    /// <summary>
    /// Name of the graph resource.
    /// </summary>
    public string? Name { get; }
    
    /// <summary>
    /// Description of the graph resource.
    /// </summary>
    public string? Description { get; }
    
    /// <summary>
    /// Collection of child routing resources.
    /// </summary>
    public IReadOnlyCollection<GraphResource> Children { get; } = [];

    /// <inheritdoc cref="GraphResource"/>
    protected GraphResource(Guid id, string? name, string? description)
    {
        Id = id;
        Name = name;
        Description = description;
    }
}