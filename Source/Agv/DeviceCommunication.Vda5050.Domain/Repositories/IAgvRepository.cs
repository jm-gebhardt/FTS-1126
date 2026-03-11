using Gebhardt.Acc.Common.Domain.Exceptions;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Models.Agvs;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Domain.Repositories;

/// <summary>
/// Provides a repository for agv-related queries.
/// </summary>
public interface IAgvRepository
{
    /// <summary>
    /// Get an agv by its correlation id.
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns>The agv.</returns>
    /// <exception cref="AgvNotFoundException"></exception>
    Task<Agv> GetAgvAsync(Guid correlationId);

    /// <summary>
    /// Add or update an agv.
    /// </summary>
    /// <param name="agv">The agv.</param>
    Task AddOrUpdateAsync(Agv agv);
}