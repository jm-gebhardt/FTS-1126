using Gebhardt.Acc.KaeserSidecar.Domain.Models;

namespace Gebhardt.Acc.KaeserSidecar.Domain.Repositories;

/// <summary>
/// Provides a repository for <see cref="JamState"/> related queries.
/// </summary>
public interface IJamStateRepository
{
    /// <summary>
    /// Add a jam state.
    /// </summary>
    /// <param name="jamState">The jam state.</param>
    Task AddAsync(JamState jamState);

    /// <summary>
    /// Update a jam state.
    /// </summary>
    /// <param name="jamState">The jam state.</param>
    Task UpdateAsync(JamState jamState);

    /// <summary>
    /// Get a jam state by station.
    /// </summary>
    /// <param name="station">The station.</param>
    /// <returns>The jam state, if existing.</returns>
    Task<JamState?> GetByStationAsync(Guid station);
}