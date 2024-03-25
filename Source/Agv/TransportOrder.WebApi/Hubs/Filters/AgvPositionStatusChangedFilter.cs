using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;

/// <summary>
/// Filter by significant changes in the position.
/// </summary>
public class AgvPositionStatusChangedFilter : InMemoryFilter<AgvPositionStatusDto>
{
    private readonly ISignificanceChecker<AgvPositionDto> _significanceChecker;

    /// <inheritdoc />
    public override TimeSpan MaxInterval => TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public AgvPositionStatusChangedFilter(
        ISignificanceChecker<AgvPositionDto> significanceChecker,
        ISemaphoreProvider<AgvPositionStatusChangedFilter> semaphoreProvider
    ) : base(semaphoreProvider)
    {
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    protected override bool ShouldActOnData(AgvPositionStatusDto currentData, AgvPositionStatusDto previousData)
    {
        return _significanceChecker.HasSignificantChanges(currentData.AgvPosition, previousData.AgvPosition);
    }
}