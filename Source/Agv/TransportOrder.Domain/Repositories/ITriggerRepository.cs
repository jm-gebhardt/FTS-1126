using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.TransportOrder.Domain.Exceptions;
using Gebhardt.Acc.TransportOrder.Domain.Models.Trigger;
using Gebhardt.Acc.TransportOrder.Domain.ReadModels;

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
    /// <param name="updateLocations">Flag if trigger locations should be updated.</param>
    Task AddOrUpdate(Trigger trigger, bool updateLocations = true);

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
    /// Update a trigger.
    /// </summary>
    /// <param name="trigger">The trigger</param>
    /// <param name="updateLocations">Flag if locations should be updated.</param>
    Task Update(Trigger trigger, bool updateLocations);

    /// <summary>
    /// Load trigger to location dictionary from repository.
    /// </summary>
    Task LoadTriggerToLocationDictionary();

    /// <summary>
    /// Get the trigger locations for a node (in memory dictionary).
    /// </summary>
    /// <param name="nodeId"></param>
    /// <returns></returns>
    ICollection<Location> GetNodeTriggerLocations(string nodeId);

    /// <summary>
    /// Get polygon trigger locations with triggers to call where the agv is located.
    /// </summary>
    /// <param name="agvId">The id of the AGV.</param>
    /// <param name="agvPosition">The position of the AGV.</param>
    /// <returns></returns>
    Task<ICollection<PolygonTriggerResult>> GetPolygonTriggerLocations(Guid agvId, AgvPosition agvPosition);

    /// <summary>
    /// Get polygon trigger locations with triggers to call where the agv is located ignoring if it was already in the polygon.
    /// </summary>
    /// <param name="agvPosition">The position of the AGV.</param>
    ICollection<Location> GetPolygonTriggerLocations(AgvPosition agvPosition);
}