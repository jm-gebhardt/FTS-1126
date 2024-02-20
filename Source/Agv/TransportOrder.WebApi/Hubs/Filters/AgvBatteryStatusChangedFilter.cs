using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;

/// <summary>
/// Filter by significant changes in the battery charge.
/// </summary>
public class AgvBatteryStatusChangedFilter : InMemoryStorageFilter<AgvBatteryStatusDto>
{
    private readonly ISignificanceChecker<AgvBatteryStatusDto> _significanceChecker;

    /// <inheritdoc />
    public override TimeSpan MinSilence => TimeSpan.FromSeconds(1);

    /// <inheritdoc />
    public override TimeSpan MaxSilence => TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public AgvBatteryStatusChangedFilter(ISignificanceChecker<AgvBatteryStatusDto> significanceChecker)
    {
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    protected override bool ShouldPublishBasedOn(AgvBatteryStatusDto currentData, AgvBatteryStatusDto previousData)
    {
        return _significanceChecker.HasSignificantChanges(currentData, previousData);
    }
}