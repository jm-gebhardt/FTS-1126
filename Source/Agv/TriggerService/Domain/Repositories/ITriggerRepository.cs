using Gebhardt.Acc.Common.TriggerDefinition;

namespace Gebhardt.Acc.TriggerService.Domain.Repositories;

/// <summary>
/// Provides a repository for triggers.
/// </summary>
public interface ITriggerRepository
{
    /// <summary>
    /// Try to add a ffcTrigger to the repository.
    /// </summary>
    /// <param name="id">The id of the ffcTrigger.</param>
    /// <param name="ffcTrigger">The ffcTrigger.</param>
    /// <returns></returns>
    public Task<bool> TryAdd(Guid id, IFfcTrigger ffcTrigger);

    /// <summary>
    /// Add or update a ffcTrigger to the repository.
    /// </summary>
    /// <param name="id">The id of the ffcTrigger.</param>
    /// <param name="ffcTrigger">The ffcTrigger.</param>
    public Task AddOrUpdate(Guid id, IFfcTrigger ffcTrigger);

    /// <summary>
    /// Get a trigger by its id.
    /// </summary>
    /// <param name="id">The trigger id.</param>
    /// <returns>The trigger</returns>
    public Task<IFfcTrigger> GetById(Guid id);

    /// <summary>
    /// Update a ffcTrigger.
    /// </summary>
    /// <param name="id">The ffcTrigger id</param>
    /// <param name="ffcTrigger">The ffcTrigger</param>
    public Task UpdateTrigger(Guid id, IFfcTrigger ffcTrigger);
}