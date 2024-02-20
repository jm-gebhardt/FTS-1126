using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
using Gebhardt.StoreWare.Agv.Common.Services;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Handles <see cref="AgvPositionChanged"/> publishing
/// </summary>
public abstract class BaseAgvPositionRepository : IAgvPositionRepository
{
    private const int MaxSilenceInSeconds = 1;
    private readonly IBus _bus;
    private readonly ISignificanceChecker<AgvPosition?> _significanceChecker;

    /// <summary>
    /// Constructor
    /// </summary>
    protected BaseAgvPositionRepository(IBus bus, ISignificanceChecker<AgvPosition?> significanceChecker)
    {
        _bus = bus;
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    public async Task UpdatePositionAsync(AgvPositionStatus agvPositionStatus)
    {
        var correlationId = agvPositionStatus.CorrelationId;
        var previousAgvPosition = GetPosition(correlationId);

        if (previousAgvPosition == null ||
            agvPositionStatus.CreatedDate.AddSeconds(-MaxSilenceInSeconds) >= previousAgvPosition.CreatedDate ||
            _significanceChecker.HasSignificantChanges(previousAgvPosition.AgvPosition, agvPositionStatus.AgvPosition))
        {
            UpdateInternal(agvPositionStatus);

            var agvPositionChanged = new AgvPositionChanged(correlationId, agvPositionStatus.AgvPosition, previousAgvPosition?.AgvPosition, agvPositionStatus.SerialNumber);
            await _bus.Publish(agvPositionChanged);
        }
    }

    /// <summary>
    /// The implementation that updates the stored status
    /// </summary>
    /// <param name="agvPositionStatus">The new agv position to</param>
    protected abstract void UpdateInternal(AgvPositionStatus agvPositionStatus);

    /// <inheritdoc />
    public abstract AgvPositionStatus? GetPosition(Guid correlationId);

    /// <inheritdoc />
    public abstract ICollection<AgvPositionStatus> GetAllPositions();

    /// <inheritdoc />
    public abstract ICollection<AgvPositionStatus> GetSelectedAgvsPositions(ICollection<Guid> correlationIds);
}