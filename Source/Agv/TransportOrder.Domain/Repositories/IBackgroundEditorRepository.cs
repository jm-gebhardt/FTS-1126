using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.BackgroundEditor;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for storing entities needed by the background editor.
/// </summary>
public interface IBackgroundEditorRepository
{
    /// <summary>
    /// Add or update a background.
    /// </summary>
    /// <param name="background">The new background.</param>
    Task AddOrUpdateBackgroundAsync(Background background);

    /// <summary>
    /// Get a background.
    /// </summary>
    /// <param name="id">The id of the background is on.</param>
    /// <returns>A background</returns>
    /// <exception cref="BackgroundNotFoundException">Raised, if there is no background with the given name/mapId.</exception>
    Task<Background> GetRequiredBackgroundAsync(Guid id);

    /// <summary>
    /// Delete a background.
    /// </summary>
    /// <param name="id">The id of the background is on.</param>
    /// <exception cref="BackgroundNotFoundException">Raised, if there is no background with the given id.</exception>
    Task DeleteBackgroundAsync(Guid id);

    /// <summary>
    /// Get backgrounds, optionally for given maps.
    /// </summary>
    /// <param name="mapIds">Optional collection of map ids.</param>
    /// <param name="maxCount">Maximum number of backgrounds returned.</param>
    /// <returns>Collection of backgrounds.</returns>
    Task<ICollection<Background>> GetBackgroundsAsync(ICollection<Guid> mapIds, int maxCount=50);
}