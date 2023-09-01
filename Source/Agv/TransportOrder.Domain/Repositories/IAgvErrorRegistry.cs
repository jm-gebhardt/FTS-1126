using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using JetBrains.Annotations;

namespace TransportOrder.Domain.Repositories;

/// <summary>
/// Registry for saving errors of Agv
/// </summary>
public interface IAgvErrorRegistry
{
    /// <summary>
    /// If error type in AgvErrorStatus error list is not existing in registry, we save the new error
    /// If error type in registry is no longer included in AgvErrorStatus error list, we mark it as resolved
    /// </summary>
    /// <param name="errors"></param>
    /// <param name="agvCorrelationId"></param>
    /// <returns></returns>
    public Task UpdateAgvErrorStatusAsync(List<AgvError>? errors, Guid agvCorrelationId);

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