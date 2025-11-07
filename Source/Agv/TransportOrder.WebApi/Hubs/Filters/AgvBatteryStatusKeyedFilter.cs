using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters.Semaphores;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters;

/// <summary>
/// Filter by significant changes in the agv battery.
/// </summary>
public class AgvBatteryStatusKeyedFilter(
    ISemaphoreProvider<IAgvBatteryStatusDtoSemaphore> semaphoreProvider,
    IConcurrentDictionaryProvider<IMessageFilter<AgvBatteryStatusDto>> concurrentDictionaryProvider,
    ISignificanceChecker<AgvBatteryStatusDto> significanceChecker,
    IKeyedSemaphoreProvider<IAgvBatteryStatusDtoSemaphore> guidSemaphoreProvider,
    AgvBatteryConfiguration configuration
)
    : KeyedInMemoryFilter<AgvBatteryStatusDto>(semaphoreProvider,
        concurrentDictionaryProvider)
{
    /// <inheritdoc />
    protected override Guid GetKey(AgvBatteryStatusDto value) => value.CorrelationId;

    /// <inheritdoc />
    protected override IMessageFilter<AgvBatteryStatusDto> GetFilter(AgvBatteryStatusDto value) => new AgvBatteryStatusChangedFilter(significanceChecker, guidSemaphoreProvider.Get(value.CorrelationId), configuration, value);
}