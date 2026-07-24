using Gebhardt.Acc.TransportOrder.Domain.Models.NewGraph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Models;

/// <summary>
/// Persistence-only join row recording that two <see cref="RoutingResource"/>s conflict.
/// <see cref="RoutingResource.ConflictingResources"/> is a symmetric self-reference and isn't representable as a
/// normal EF navigation, so it's stored as plain ids here and resolved explicitly by GraphRepository, which attaches
/// both directions in memory via <see cref="RoutingResource.AddConflictingResource"/>.
/// </summary>
[UsedImplicitly]
public class RoutingResourceConflict
{
    /// <summary>Id of one of the two conflicting routing resources.</summary>
    [UsedImplicitly]
    public Guid ResourceId { get; set; }

    /// <summary>Id of the other conflicting routing resource.</summary>
    [UsedImplicitly]
    public Guid ConflictingResourceId { get; set; }
}
