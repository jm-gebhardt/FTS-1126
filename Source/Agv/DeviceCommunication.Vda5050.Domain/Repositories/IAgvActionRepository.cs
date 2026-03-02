using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Action;

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
}