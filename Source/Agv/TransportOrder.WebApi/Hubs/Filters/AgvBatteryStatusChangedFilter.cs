using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters;

/// <summary>
/// Filter by significant changes in the battery charge.
/// </summary>
public class AgvBatteryStatusChangedFilter : InMemoryFilter<AgvBatteryStatusDto>
{
    private readonly ISignificanceChecker<AgvBatteryStatusDto> _significanceChecker;

    /// <inheritdoc />
    public AgvBatteryStatusChangedFilter(
        ISignificanceChecker<AgvBatteryStatusDto> significanceChecker,
        ISemaphoreProvider semaphoreProvider,
        AgvBatteryConfiguration agvBatterConfiguration,
        AgvBatteryStatusDto? currentData = null
    ) : base(semaphoreProvider, agvBatterConfiguration, currentData)
    {
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    protected override bool ShouldActOnData(AgvBatteryStatusDto currentData, AgvBatteryStatusDto previousData)
    {
        return _significanceChecker.HasSignificantChanges(currentData, previousData);
    }
}