using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Services.Significance;

namespace Gebhardt.StoreWare.Agv.Common.Services.Filters;

/// <summary>
/// Filter by significant changes in the position.
/// </summary>
public class AgvPositionReceivedFilter : InMemoryFilter<AgvPositionReceived>
{
    private readonly ISignificanceChecker<AgvPosition> _significanceChecker;

    /// <inheritdoc />
    public AgvPositionReceivedFilter(
        ISignificanceChecker<AgvPosition> significanceChecker,
        ISemaphoreProvider semaphoreProvider,
        AgvPositionReceived currentData,
        AgvPositionConfiguration configuration
    ) : base(semaphoreProvider, currentData, configuration)
    {
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    protected override bool ShouldActOnData(AgvPositionReceived currentData, AgvPositionReceived previousData)
    {
        return _significanceChecker.HasSignificantChanges(currentData.AgvPosition, previousData.AgvPosition);
    }
}