using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.AgvActions;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;

/// <summary>
/// Provides a repository for <see cref="AgvAction"/> related queries.
/// </summary>
public interface IAgvActionRepository
{
    /// <summary>
    /// Add or update agv actions.
    /// </summary>
    /// <param name="actions">The agv actions.</param>
    /// <param name="agvSerialNumber">The serial number of the agv.</param>
    Task AddOrUpdateActionsAsync(ICollection<AgvAction> actions, string agvSerialNumber);

    /// <summary>
    /// Get all actions for this agv on given scope.
    /// </summary>
    /// <param name="agvSerialNumber">The agv serial number.</param>
    /// <param name="scope">The action scope.</param>
    /// <returns>A collection of actions the agv can run on the given scope.</returns>
    Task<ICollection<AgvAction>> GetActionsForScopeAsync(string agvSerialNumber, ActionScope scope);
}