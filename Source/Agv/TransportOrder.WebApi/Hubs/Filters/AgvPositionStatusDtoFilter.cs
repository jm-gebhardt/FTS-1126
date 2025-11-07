using Gebhardt.Acc.Common.Services;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.Common.Services.Significance;
using Gebhardt.Acc.TransportOrder.WebApi.Dtos.Agv;

namespace Gebhardt.Acc.TransportOrder.WebApi.Hubs.Filters;

/// <summary>
/// Filter by significant changes in the position.
/// </summary>
public class AgvPositionStatusDtoFilter : InMemoryFilter<AgvPositionStatusDto>
{
    private readonly ISignificanceChecker<AgvPositionDto> _significanceChecker;

    /// <inheritdoc />
    public AgvPositionStatusDtoFilter(
        ISignificanceChecker<AgvPositionDto> significanceChecker,
        ISemaphoreProvider semaphoreProvider,
        AgvPositionStatusDto currentData,
        AgvPositionConfiguration agvPositionConfiguration
    ) : base(semaphoreProvider, agvPositionConfiguration, currentData)
    {
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    protected override bool ShouldActOnData(AgvPositionStatusDto currentData, AgvPositionStatusDto previousData)
    {
        return _significanceChecker.HasSignificantChanges(currentData.AgvPosition, previousData.AgvPosition);
    }
}