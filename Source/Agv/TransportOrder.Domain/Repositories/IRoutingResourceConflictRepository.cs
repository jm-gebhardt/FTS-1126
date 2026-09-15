using Gebhardt.Acc.TransportOrder.Domain.Models.Graph;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provide access to the persisted conflicts between routing resources.
/// </summary>
[PublicAPI]
public interface IRoutingResourceConflictRepository
{
    /// <summary>
    /// Returns all persisted conflict pairs.
    /// </summary>
    Task<IReadOnlyList<ResourceConflict>> GetAllAsync();

    /// <summary>
    /// Returns all persisted conflict pairs that involve the given resource.
    /// </summary>
    /// <param name="resourceId">The identifier of the resource.</param>
    Task<IReadOnlyList<ResourceConflict>> GetByResourceAsync(Guid resourceId);

    /// <summary>
    /// Replaces every persisted conflict pair with the given ones.
    /// </summary>
    /// <param name="conflicts">The complete set of conflict pairs.</param>
    Task SetConflictsAsync(IReadOnlyCollection<ResourceConflict> conflicts);
}