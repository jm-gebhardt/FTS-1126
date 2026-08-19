using Gebhardt.Acc.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TransportOrder.Domain.Repositories;

/// <summary>
/// Repository for saving errors of an agv.
/// </summary>
public interface IAgvErrorRepository
{
    /// <summary>
    /// Updates the repository with new errors and marks resolved errors for the specified AGV.
    /// </summary>
    /// <param name="newErrors">List of new errors to be added.</param>
    /// <param name="resolvedErrors">List of errors to mark as resolved.</param>
    /// <param name="agvCorrelationId">The CorrelationId of the Agv</param>
    Task AddAndUpdateAgvErrorAsync(List<AgvError> newErrors, List<AgvError> resolvedErrors, Guid agvCorrelationId);

    /// <summary>
    /// Gets all active errors for given Agv excluding the resolved errors
    /// </summary>
    /// <param name="agvCorrelationId">The CorrelationId of the Agv</param>
    /// <returns>List of active errors for the given AGV.</returns>
    Task<IReadOnlyList<AgvError>> GetActiveErrorsAsync(Guid agvCorrelationId);

    /// <summary>
    /// Gets all active errors for given Agvs excluding the resolved errors
    /// </summary>
    /// <param name="agvCorrelationIds">The CorrelationIds of the Agv</param>
    /// <returns>List of active errors for the given AGV.</returns>
    Task<IReadOnlyList<AgvError>> GetActiveErrorsAsync(IList<Guid> agvCorrelationIds);

    /// <summary>
    /// Gets all previous errors for given Agv including resolved errors
    /// </summary>
    /// <param name="agvCorrelationId">The CorrelationId of the Agv</param>
    /// <param name="dateFrom">Only consider errors newer than this parameter (if set)</param>
    /// <param name="dateTo">Only consider errors older than this parameter (if set)</param>
    /// <returns>List of all errors for the given AGV and time range.</returns>
    Task<IReadOnlyList<AgvError>> GetErrorsAsync(Guid agvCorrelationId, DateTime dateFrom, DateTime dateTo);

    /// <summary>
    /// Gets all previous errors for given Agvs including resolved errors
    /// </summary>
    /// <param name="agvCorrelationIds">The CorrelationIds of the Agv</param>
    /// <param name="dateFrom">Only consider errors newer than this parameter (if set)</param>
    /// <param name="dateTo">Only consider errors older than this parameter (if set)</param>
    /// <returns>List of all errors for the given AGVs and time range.</returns>
    [PublicAPI]
    Task<IReadOnlyList<AgvError>> GetErrorsAsync(IList<Guid> agvCorrelationIds, DateTime dateFrom, DateTime dateTo);

    /// <summary>
    /// Load db agv error information into the cache.
    /// </summary>
    Task LoadInMemoryCache();

    /// <summary>
    /// Delete errors older than given time.
    /// </summary>
    /// <param name="outDatedTime">The time an error gets outdated.</param>
    Task DeleteOutdatedErrors(DateTime outDatedTime);

    /// <summary>
    /// Delete wifi information older than given time.
    /// </summary>
    /// <param name="outDatedTime">The time a wifi information gets outdated.</param>
    Task DeleteOutdatedWifiInformation(DateTime outDatedTime);
}