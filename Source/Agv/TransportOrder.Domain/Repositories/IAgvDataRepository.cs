using Gebhardt.Acc.Common.Domain.Models;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Abstracted interface for AgvDataRepository.
/// </summary>
public interface IAgvDataRepository
{
    /// <summary>
    /// Checks if AgvData related to its correlationId exists in repository
    /// </summary>
    /// <param name="correlationId">GUID of the AgvData looked for in the repository</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task<bool> ExistsAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the AgvData for an agv from the AgvDataRepository.
    /// </summary>
    /// <param name="correlationId">AGV correlationId</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns><see cref="AgvData"/> object or null if no data was found for the given correlationId</returns>
    public Task<AgvData?> GetAgvDataAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add or update a AgvData to/in the repository.
    /// </summary>
    /// <param name="agvData">AgvData</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a change has been made</returns>
    public Task AddOrUpdateAsync(AgvData agvData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the data of selected agvs.
    /// </summary>
    /// <param name="correlationIds">The correlation id of the selected agvs.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public Task<ICollection<AgvData>> GetSelectedAgvDataAsync(ICollection<Guid> correlationIds, CancellationToken cancellationToken = default);
}