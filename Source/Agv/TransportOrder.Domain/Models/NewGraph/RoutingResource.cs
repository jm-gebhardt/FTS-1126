using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;

/// <summary>
/// Represents a resource in the routing graph. Each resource has a unique identifier, an associated AGV type, and a reference to its parent graph resource. 
/// </summary>
public abstract class RoutingResource
{
    private readonly List<RoutingResource> _conflictingResources = [];

    /// <summary>
    /// Unique identifier of the routing resource.
    /// </summary>
    [UsedImplicitly]
    public Guid Id { get; }

    //public Guid ParentId { get; } needed for DB?

    /// <summary>
    /// Reference to the parent graph resource that this routing resource belongs to.
    /// </summary>
    [UsedImplicitly]
    public GraphResource Parent { get; }

    /// <summary>
    /// AgvType associated with this routing resource.
    /// </summary>
    [UsedImplicitly]
    public AgvType AgvType { get; }

    /// <summary>
    /// Collection of routing resources that cannot be used simultaneously with this resource.
    /// </summary>
    public IReadOnlyCollection<RoutingResource> ConflictingResources => _conflictingResources;

    /// <inheritdoc cref="RoutingResource"/> 
    protected RoutingResource(Guid id, GraphResource parent, AgvType agvType)
    {
        Id = id;
        Parent = parent;
        AgvType = agvType;
    }

    /// <summary>
    /// Adds a routing resource to the collection of conflicting resources.
    /// </summary>
    public void AddConflictingResource(RoutingResource resource)
    {
        _conflictingResources.Add(resource);
    }
}