using Gebhardt.Acc.Common.TriggerDefinition;

namespace Gebhardt.Acc.TriggerService.Domain.Repositories;

/// <summary>
/// Provides a repository for triggers.
/// </summary>
public interface ITriggerRepository
{
    /// <summary>
    /// Try to add a trigger to the repository.
    /// </summary>
    /// <param name="id">The id of the trigger.</param>
    /// <param name="trigger">The trigger.</param>
    /// <returns></returns>
    public Task<bool> TryAdd(Guid id, ITrigger trigger);

    /// <summary>
    /// Get a trigger by its id.
    /// </summary>
    /// <param name="id">The trigger id.</param>
    /// <returns>The trigger</returns>
    public Task<ITrigger> GetById(Guid id);

    /// <summary>
    /// Update a trigger.
    /// </summary>
    /// <param name="id">The trigger id</param>
    /// <param name="trigger">The trigger</param>
    public Task UpdateTrigger(Guid id, ITrigger trigger);
}