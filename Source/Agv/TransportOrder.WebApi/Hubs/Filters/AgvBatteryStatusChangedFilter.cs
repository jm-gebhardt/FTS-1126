using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;

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