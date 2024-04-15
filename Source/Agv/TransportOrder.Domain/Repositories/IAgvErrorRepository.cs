using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using JetBrains.Annotations;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;

/// <summary>
/// Repository for saving errors of an agv.
/// </summary>
public interface IAgvErrorRepository
{
    /// <summary>
    /// If error type in AgvErrorStatus error list is not existing in repository, we save the new error
    /// If error type in repository is no longer included in AgvErrorStatus error list, we mark it as resolved
    /// </summary>
    /// <param name="errors"></param>
    /// <param name="agvCorrelationId"></param>
    /// <returns>True if the errors have changed.</returns>
    public Task<bool> AddOrUpdateAgvErrorStatusAsync(List<AgvError>? errors, Guid agvCorrelationId);

    /// <summary>
    /// Gets all active errors for given Agv excluding the resolved errors
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <returns></returns>
    public Task<IList<AgvError>> GetActiveErrorsAsync(Guid agvCorrelationId);

    /// <summary>
    /// Gets all active errors for given Agvs excluding the resolved errors
    /// </summary>
    /// <param name="agvCorrelationIds"></param>
    /// <returns></returns>
    public Task<IList<AgvError>> GetActiveErrorsAsync(IList<Guid> agvCorrelationIds);

    /// <summary>
    /// Gets all previous errors for given Agv including resolved errors
    /// </summary>
    /// <param name="agvCorrelationId"></param>
    /// <param name="dateFrom">Only consider errors newer than this parameter (if set)</param>
    /// <param name="dateTo">Only consider errors older than this parameter (if set)</param>
    /// <returns></returns>
    public Task<IReadOnlyList<AgvError>> GetErrorsAsync(Guid agvCorrelationId, DateTime dateFrom, DateTime dateTo);

    /// <summary>
    /// Gets all previous errors for given Agvs including resolved errors
    /// </summary>
    /// <param name="agvCorrelationIds"></param>
    /// <param name="dateFrom">Only consider errors newer than this parameter (if set)</param>
    /// <param name="dateTo">Only consider errors older than this parameter (if set)</param>
    /// <returns></returns>
    [PublicAPI]
    public Task<IReadOnlyList<AgvError>> GetErrorsAsync(IList<Guid> agvCorrelationIds, DateTime dateFrom, DateTime dateTo);
}