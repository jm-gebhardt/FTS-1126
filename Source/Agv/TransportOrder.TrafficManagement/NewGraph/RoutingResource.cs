using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.TrafficManagement.NewGraph;

/// <summary>
/// Represents a resource in the routing graph. Each resource has a unique identifier, an associated AGV type, and a reference to its parent graph resource. 
/// </summary>
public abstract class RoutingResource
{
    [UsedImplicitly]
    private readonly List<RoutingResource> _conflictingResources = [];

    /// <summary>
    /// Unique identifier of the routing resource.
    /// </summary>
    [UsedImplicitly]
    public Guid Id { get; }

    /// <summary>
    /// Reference to the parent graph resource that this routing resource belongs to.
    /// </summary>
    [UsedImplicitly]
    public GraphResource Parent { get; private set; }

    /// <summary>
    /// AgvType associated with this routing resource.
    /// </summary>
    [UsedImplicitly]
    public AgvType AgvType { get; }

    /// <summary>
    /// Collection of routing resources that cannot be used simultaneously with this resource.
    /// </summary>
    [UsedImplicitly]
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
    [UsedImplicitly]
    public void AddConflictingResource(RoutingResource resource)
    {
        _conflictingResources.Add(resource);
    }

    /// <summary>
    /// Attaches the parent graph resource. Not backed by an EF relation (see <c>RoutingResourceEntityConfiguration</c>) -
    /// used by GraphRepository to assemble the aggregate after resolving Parent/Children via an explicit join.
    /// </summary>
    [UsedImplicitly]
    public void AttachParent(GraphResource parent)
    {
        Parent = parent;
    }
}