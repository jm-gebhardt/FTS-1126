using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.StoreWare.Agv.TransportOrder.WebApi.Hubs.Filters;

/// <summary>
/// Filter by significant changes in the position.
/// </summary>
public class AgvPositionStatusChangedFilter : InMemoryStorageFilter<AgvPositionStatusDto>
{
    private readonly ISignificanceChecker<AgvPositionDto> _significanceChecker;

    /// <inheritdoc />
    public override TimeSpan MaxSilence => TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public AgvPositionStatusChangedFilter(ISignificanceChecker<AgvPositionDto> significanceChecker)
    {
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    protected override bool ShouldPublishBasedOn(AgvPositionStatusDto currentData, AgvPositionStatusDto previousData)
    {
        return _significanceChecker.HasSignificantChanges(currentData.AgvPosition, previousData.AgvPosition);
    }
}