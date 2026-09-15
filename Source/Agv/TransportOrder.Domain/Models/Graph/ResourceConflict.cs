using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Models.Graph;

/// <summary>
/// A pair of routing resources that are in conflict with each other.
/// </summary>
/// <param name="ResourceId">The identifier of the first resource.</param>
/// <param name="ConflictingResourceId">The identifier of the second resource.</param>
[PublicAPI]
public record ResourceConflict(Guid ResourceId, Guid ConflictingResourceId);