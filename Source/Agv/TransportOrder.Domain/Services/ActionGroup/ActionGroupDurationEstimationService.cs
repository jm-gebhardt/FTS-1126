using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Domain.Services.ActionGroup;

/// <inheritdoc/>
public class ActionGroupDurationEstimationService : IActionGroupDurationEstimationService
{
    private readonly IConcurrentDictionaryProvider<string, (double value, DateTime lastUpdated)> _deviationCache;
    private readonly IActionGroupRepository _actionGroupRepository;
    private readonly TimeSpan _updateSpan = TimeSpan.FromHours(1);

    /// <inheritdoc cref="ActionGroupDurationEstimationService"/>
    public ActionGroupDurationEstimationService(IConcurrentDictionaryProvider<string, (double, DateTime)> deviationCache, IActionGroupRepository actionGroupRepository)
    {
        _deviationCache = deviationCache;
        _actionGroupRepository = actionGroupRepository;
    }

    /// <inheritdoc/>
    public async Task<double> GetAverageDurationEstimateDeviationAsync(Guid agvCorrelationId, string[] targetNodes)
    {
        string key = GetKey(agvCorrelationId, targetNodes);
        bool deviationExists = _deviationCache.ConcurrentDict.TryGetValue(key, out var deviation);
        var now = DateTime.UtcNow;
        if (!deviationExists || now - deviation.lastUpdated > _updateSpan)
        {
            deviation.value = await _actionGroupRepository.GetAverageDurationEstimateDeviationAsync(agvCorrelationId, targetNodes);
            deviation.lastUpdated = now;
            _deviationCache.ConcurrentDict.AddOrUpdate(key, deviation, (_, _) => deviation);
        }

        return deviation.value;
    }

    /// <summary>
    /// Get key of in memory cache of deviations.
    /// </summary>
    /// <param name="agvCorrelationId">The agv correlation id.</param>
    /// <param name="targetNodes">The target nodes.</param>
    /// <returns></returns>
    public static string GetKey(Guid agvCorrelationId, string[] targetNodes)
    {
        return $"{agvCorrelationId}-{string.Join("-", targetNodes.OrderDescending())}";
    }
}