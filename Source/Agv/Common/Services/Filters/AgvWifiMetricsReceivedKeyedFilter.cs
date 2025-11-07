using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Domain.SemaphoreProviders;
using Gebhardt.Acc.Common.Messages.AgvManagement.Events;
using Gebhardt.Acc.Common.Services.Significance;

namespace Gebhardt.Acc.Common.Services.Filters;

/// <summary>
/// In-memory filter for <see cref="AgvWifiMetricsReceived"/> messages that keeps track of previously seen messages.
/// </summary>
public class AgvWifiMetricsReceivedKeyedFilter(
    ISemaphoreProvider<IAgvWifiMetricsReceivedSemaphoreProvider> semaphoreProvider,
    IConcurrentDictionaryProvider<IMessageFilter<AgvWifiMetricsReceived>> concurrentDictionaryProvider,
    ISignificanceChecker<WifiMetrics> significanceChecker,
    IKeyedSemaphoreProvider<AgvWifiMetricsReceived> guidSemaphoreProvider,
    WifiConfiguration configuration
)
    : KeyedInMemoryFilter<AgvWifiMetricsReceived>(semaphoreProvider,
        concurrentDictionaryProvider)
{
    /// <inheritdoc />
    protected override Guid GetKey(AgvWifiMetricsReceived value) => value.CorrelationId;

    /// <inheritdoc />
    protected override IMessageFilter<AgvWifiMetricsReceived> GetFilter(AgvWifiMetricsReceived value) => new AgvWifiMetricsReceivedFilter(significanceChecker, guidSemaphoreProvider.Get(value.CorrelationId), value, configuration);
}