using Gebhardt.StoreWare.Agv.Common.Dictionaries;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.SemaphoreProviders;
using Gebhardt.StoreWare.Agv.Common.Services.Significance;

namespace Gebhardt.StoreWare.Agv.Common.Services.Filters;

/// <summary>
/// Compares <see cref="AgvPositionReceived"/> (separated by Guid( in memory and uses <see cref="AgvPositionReceivedFilter"/> to detect if it should be acted on.
/// </summary>
/// <param name="semaphoreProvider"></param>
/// <param name="concurrentDictionaryProvider"></param>
/// <param name="significanceChecker"></param>
/// <param name="guidSemaphoreProvider"></param>
/// <param name="configuration"></param>
public class AgvPositionReceivedInMemoryFilter(
    ISemaphoreProvider<IAgvPositionsReceivedSemaphoreProvider> semaphoreProvider,
    IConcurrentDictionaryProvider<IMessageFilter<AgvPositionReceived>> concurrentDictionaryProvider,
    ISignificanceChecker<AgvPosition> significanceChecker,
    IKeyedSemaphoreProvider<IAgvPositionsReceivedSemaphoreProvider> guidSemaphoreProvider,
    AgvPositionConfiguration configuration
)
    : KeyedInMemoryFilter<AgvPositionReceived>(semaphoreProvider,
        concurrentDictionaryProvider)
{
    /// <inheritdoc />
    protected override Guid GetKey(AgvPositionReceived value) => value.CorrelationId;

    /// <inheritdoc />
    protected override IMessageFilter<AgvPositionReceived> GetFilter(AgvPositionReceived value) => new AgvPositionReceivedFilter(significanceChecker, guidSemaphoreProvider.Get(value.CorrelationId), value, configuration);
}