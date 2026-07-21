using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Dtos;

namespace Gebhardt.Acc.DeviceCommunication.Vda5050.Simulator.WebApi.Hubs.Filters;

/// <summary>
/// Filters by significant changes in the VDA5050 state.
/// </summary>
public class Vda5050StateDtoFilter : InMemoryFilter<Vda5050StateDto>
{
    private readonly ISignificanceChecker<Vda5050StateDto> _significanceChecker;

    /// <inheritdoc cref="Vda5050StateDtoFilter"/>
    public Vda5050StateDtoFilter(
        ISignificanceChecker<Vda5050StateDto> significanceChecker,
        ISemaphoreProvider semaphoreProvider,
        Vda5050StateDto currentData,
        Vda5050StateConfiguration configuration
    ) : base(semaphoreProvider, configuration, currentData)
    {
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    protected override bool ShouldActOnData(Vda5050StateDto currentData, Vda5050StateDto previousData)
    {
        return _significanceChecker.HasSignificantChanges(currentData, previousData);
    }
}