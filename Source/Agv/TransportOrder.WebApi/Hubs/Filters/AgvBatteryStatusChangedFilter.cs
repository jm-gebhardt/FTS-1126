using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters;

/// <summary>
/// Filter by significant changes in the battery charge.
/// </summary>
public class AgvBatteryStatusChangedFilter : InMemoryFilter<AgvBatteryStatusDto>
{
    private readonly ISignificanceChecker<AgvBatteryStatusDto> _significanceChecker;

    /// <inheritdoc />
    public override TimeSpan MinInterval => TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public override TimeSpan MaxInterval => TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public AgvBatteryStatusChangedFilter(
        ISignificanceChecker<AgvBatteryStatusDto> significanceChecker,
        ISemaphoreProvider<AgvBatteryStatusChangedFilter> semaphoreProvider,
        AgvBatteryStatusDto? currentData = null
    ) : base(semaphoreProvider, currentData)
    {
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    protected override bool ShouldActOnData(AgvBatteryStatusDto currentData, AgvBatteryStatusDto previousData)
    {
        return _significanceChecker.HasSignificantChanges(currentData, previousData);
    }
}