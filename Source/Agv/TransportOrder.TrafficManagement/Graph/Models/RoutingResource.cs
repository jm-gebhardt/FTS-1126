using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.Graph.Models;

/// <summary>
/// Represents a resource in the routing graph. Each resource has a unique identifier, an associated AGV type, and a reference to its parent graph resource. 
/// </summary>
public abstract class RoutingResource
{
    private readonly List<RoutingResource> _conflictingResources = [];

    /// <summary>
    /// Unique identifier of the routing resource.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Reference to the parent graph resource that this routing resource belongs to.
    /// </summary>
    public GraphResource Parent { get; private set; }

    /// <summary>
    /// AgvType associated with this routing resource.
    /// </summary>
    public AgvType AgvType { get; }

    /// <summary>
    /// Collection of routing resources that cannot be used simultaneously with this resource.
    /// </summary>
    public IReadOnlyCollection<RoutingResource> ConflictingResources => _conflictingResources;
    
    /// <summary>
    /// Human-readable description of the routing resource, which can be used for logging.
    /// </summary>
    public abstract string Description { get; }

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

    public override string ToString()
    {
        return Description;
    }
}