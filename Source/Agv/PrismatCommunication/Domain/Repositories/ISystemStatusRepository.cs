using Gebhardt.Acc.Common.Domain.Repositories;
using Gebhardt.Acc.PrismatCommunication.Domain.Models.SystemStatus;

namespace Gebhardt.Acc.PrismatCommunication.Domain.Repositories;

/// <summary>
/// Provides a repository for system status related queries.
/// </summary>
public interface ISystemStatusRepository : IRepository<SystemStatus>
{
    /// <summary>
    /// Get the system status.
    /// </summary>
    /// <returns>The system status if it exists.</returns>
    Task<SystemStatus?> GetStatus();

    /// <summary>
    /// Update the system status.
    /// </summary>
    /// <param name="status"></param>
    Task UpdateAsync(SystemStatus status);
}