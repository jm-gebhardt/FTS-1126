using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;
using Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters.Semaphores;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters;

/// <summary>
/// Filter by significant changes in the position.
/// </summary>
public class AgvPositionStatusKeyedFilter(
    ISemaphoreProvider<IAgvPositionStatusDtoSemaphore> semaphoreProvider,
    IConcurrentDictionaryProvider<IMessageFilter<AgvPositionStatusDto>> concurrentDictionaryProvider,
    ISignificanceChecker<AgvPositionDto> significanceChecker,
    IKeyedSemaphoreProvider<IAgvPositionStatusDtoSemaphore> guidSemaphoreProvider,
    AgvPositionConfiguration configuration
)
    : KeyedInMemoryFilter<AgvPositionStatusDto>(semaphoreProvider,
        concurrentDictionaryProvider)
{
    /// <inheritdoc />
    protected override Guid GetKey(AgvPositionStatusDto value) => value.CorrelationId;

    /// <inheritdoc />
    protected override IMessageFilter<AgvPositionStatusDto> GetFilter(AgvPositionStatusDto value) => new AgvPositionStatusDtoFilter(significanceChecker, guidSemaphoreProvider.Get(value.CorrelationId), value, configuration);
}