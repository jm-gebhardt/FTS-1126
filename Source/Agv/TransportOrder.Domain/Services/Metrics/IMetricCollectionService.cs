using Gebhardt.Acc.Common.Domain.Models;
using AgvStatus = Gebhardt.Acc.TransportOrder.Domain.Models.Agv.AgvStatus;

namespace Gebhardt.Acc.TransportOrder.Domain.Services.Metrics;

/// <summary>
/// Provides methods to collect metrics.
/// </summary>
public interface IMetricCollectionService
{
    /// <summary>
    /// Collect metrics on order completion.
    /// </summary>
    /// <param name="actionGroup">The action group.</param>
    /// <param name="lastNodeId">The last node id of the agv.</param>
    Task CollectOrderCompletionMetrics(Models.ActionGroup.ActionGroup actionGroup, Guid? lastNodeId = null);

    /// <summary>
    /// Collect agv battery charge metrics.
    /// </summary>
    /// <param name="agvStatus"></param>
    void CollectBatteryChargeMetrics(AgvStatus? agvStatus);

    /// <summary>
    /// Collect agv safety field violation metrics
    /// </summary>
    /// <param name="agvCorrelationId">The correlation id of the agv.</param>
    /// <param name="newErrors">The list of new errors.</param>
    /// <param name="resolvedErrors">The list of resolved errors.</param>
    void CollectSafetyFieldViolationMetrics(Guid agvCorrelationId, List<AgvError> newErrors, List<AgvError> resolvedErrors);

    /// <summary>
    /// Collect station jam state metrics.
    /// </summary>
    /// <param name="stationId">The station id.</param>
    /// <param name="lastChangedDate">The time the jam state changed last.</param>
    /// <param name="previousHasPalletJam">The previous jam state.</param>
    /// <param name="currentChangedDate">The current jam state change time.</param>
    /// <param name="currentHasPalletJam">The current jam state.</param>
    void CollectJamStateMetrics(Guid stationId, DateTime lastChangedDate, bool previousHasPalletJam, DateTime currentChangedDate, bool currentHasPalletJam);
}