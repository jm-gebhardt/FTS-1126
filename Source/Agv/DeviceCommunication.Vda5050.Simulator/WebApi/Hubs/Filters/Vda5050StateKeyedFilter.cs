using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs.Filters.Semaphores;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs.Filters;

/// <summary>
/// Filters VDA5050 state (per Agv) for significant changes.
/// </summary>
public class Vda5050StateKeyedFilter(
    ISemaphoreProvider<ISimulatedAgvStateSemaphore> semaphoreProvider,
    IConcurrentDictionaryProvider<IMessageFilter<Vda5050StateDto>> concurrentDictionaryProvider,
    ISignificanceChecker<Vda5050StateDto> significanceChecker,
    IKeyedSemaphoreProvider<ISimulatedAgvStateSemaphore> guidSemaphoreProvider,
    Vda5050StateConfiguration configuration
) : KeyedInMemoryFilter<Vda5050StateDto>(semaphoreProvider, concurrentDictionaryProvider)
{
    /// <inheritdoc />
    protected override Guid GetKey(Vda5050StateDto value)
    {
        return value.CorrelationId;
    }

    /// <inheritdoc />
    protected override IMessageFilter<Vda5050StateDto> GetFilter(Vda5050StateDto value)
    {
        return new Vda5050StateDtoFilter(significanceChecker, guidSemaphoreProvider.Get(value.CorrelationId), value, configuration);
    }
}