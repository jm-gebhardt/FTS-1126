using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Significance;

namespace Gebhardt.Acc.Common.Services.Filters;

/// <summary>
/// Filter for <see cref="AgvWifiMetricsReceived"/> events based on significance of the Wi-Fi metrics.
/// </summary>
public class AgvWifiMetricsReceivedFilter : InMemoryFilter<AgvWifiMetricsReceived>
{
    private readonly ISignificanceChecker<WifiMetrics> _significanceChecker;

    /// <inheritdoc />
    public AgvWifiMetricsReceivedFilter(
        ISignificanceChecker<WifiMetrics> significanceChecker,
        ISemaphoreProvider semaphoreProvider,
        AgvWifiMetricsReceived currentData,
        WifiConfiguration configuration
    ) : base(semaphoreProvider, currentData, configuration)
    {
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    protected override bool ShouldActOnData(AgvWifiMetricsReceived currentData, AgvWifiMetricsReceived previousData)
    {
        return _significanceChecker.HasSignificantChanges(currentData.Metrics, previousData.Metrics);
    }
}