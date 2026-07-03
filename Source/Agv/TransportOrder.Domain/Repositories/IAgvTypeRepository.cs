using Gebhardt.Acc.TransportOrder.Domain.Models.AgvTypes;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for <see cref="AgvType"/>.
/// </summary>
public interface IAgvTypeRepository
{
    /// <summary>
    /// Checks whether an AGV type with the given identifier exists.
    /// </summary>
    /// <param name="agvTypeIdentifier">The AGV type identifier.</param>
    /// <returns>True if an AGV type with the identifier exists.</returns>
    public Task<bool> ExistsAsync(string agvTypeIdentifier);

    /// <summary>
    /// Adds a new AGV type or updates the existing one with the same identifier.
    /// </summary>
    /// <param name="agvType">The AGV type to add or update.</param>
    public Task AddOrUpdateAsync(AgvType agvType);
}