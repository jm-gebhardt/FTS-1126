using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Provides a repository for trigger related queries
/// </summary>
public interface ITriggerRepository
{
    /// <summary>
    /// Add trigger.
    /// </summary>
    /// <param name="trigger">The trigger to be added.</param>
    Task Add(Trigger trigger);

    /// <summary>
    /// Get all triggers.
    /// </summary>
    /// <returns>A collection of triggers.</returns>
    Task<ICollection<Trigger>> GetTriggers();

    /// <summary>
    /// Get a trigger by its id.
    /// </summary>
    /// <param name="triggerId">The trigger id.</param>
    /// <returns>The trigger.</returns>
    /// <exception cref="TriggerNotFoundException">Thrown when the trigger was not found using this id.</exception>
    Task<Trigger> GetById(Guid triggerId);

    /// <summary>
    /// Get ids of trigger which are assigned to a node (in memory dictionary).
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    ICollection<Guid> GetTriggersAssignedToNode(string nodeId);

    /// <summary>
    /// Update a trigger.
    /// </summary>
    /// <param name="trigger">The trigger</param>
    Task Update(Trigger trigger);

    /// <summary>
    /// Load trigger to location dictionary from repository.
    /// </summary>
    Task LoadTriggerToLocationDictionary();
}