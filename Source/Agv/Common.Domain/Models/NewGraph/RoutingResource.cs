using JetBrains.Annotations;

namespace Gebhardt.Acc.Common.Domain.Models.NewGraph;

/// <summary>
/// Represents a resource in the routing graph. Each resource has a unique identifier, an associated AGV type, and a reference to its parent graph resource. 
/// </summary>
[PublicAPI]
public abstract record RoutingResource
{
    /// <summary>
    /// Unique identifier of the routing resource.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// AgvType associated with this routing resource.
    /// </summary>
    public Guid AgvTypeId { get; init; }

    /// <inheritdoc cref="RoutingResource"/>
    protected RoutingResource(Guid id, Guid agvTypeId)
    {
        Id = id;
        AgvTypeId = agvTypeId;
    }
}