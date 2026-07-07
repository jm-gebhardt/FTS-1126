using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
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
    /// Checks whether an AGV type with the given id.
    /// </summary>
    /// <param name="id">The AGV type id.</param>
    /// <returns>True if an AGV type with the id exists.</returns>
    public Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// Adds a new AGV type or updates the existing one with the same identifier.
    /// </summary>
    /// <param name="agvType">The AGV type to add or update.</param>
    /// <param name="cancellationToken">TThe cancellation token.</param>
    public Task AddOrUpdateAsync(AgvType agvType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an agv type by id.
    /// </summary>
    /// <param name="id">The id of the agv type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An agv type.</returns>
    /// <exception cref="AgvTypeNotFoundException">Raised, when the agv type could not be found with the given id.</exception>
    public Task<AgvType> GetAgvTypeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all agv types .
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of agv types.</returns>
    public Task<ICollection<AgvType>> GetAllAgvTypesAsync(CancellationToken cancellationToken = default);
}