using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement;
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
    private const float PositionThreshold = 0.005f;
    private const float DeviationRangeThreshold = 0.001f;
    private const int MaxSilenceInSeconds = 1;
    private readonly IBus _bus;

    /// <summary>
    /// Constructor
    /// </summary>
    protected BaseAgvPositionRepository(IBus bus)
    {
        _bus = bus;
    }

    private static bool Equals(AgvPosition? agvPosition1, AgvPosition agvPosition2)
    {
        if (agvPosition1 == null)
        {
            return false;
        }

        bool equals =
            agvPosition1.PositionInitialized == agvPosition2.PositionInitialized
            && Math.Abs(agvPosition1.X - agvPosition2.X) <= PositionThreshold
            && Math.Abs(agvPosition1.Y - agvPosition2.Y) <= PositionThreshold
            && Math.Abs(agvPosition1.Z - agvPosition2.Z) <= PositionThreshold
            && Math.Abs(agvPosition1.Theta - agvPosition2.Theta) <= PositionThreshold
            && Math.Abs(agvPosition1.DeviationRange.GetValueOrDefault() - agvPosition2.DeviationRange.GetValueOrDefault()) < DeviationRangeThreshold;

        return equals;
    }

    /// <inheritdoc />
    public async Task UpdatePositionAsync(AgvPositionStatus agvPositionStatus)
    {
        var correlationId = agvPositionStatus.CorrelationId;
        var previousAgvPosition = GetPosition(correlationId);

        if (!Equals(previousAgvPosition?.AgvPosition, agvPositionStatus.AgvPosition) || previousAgvPosition!.CreatedDate < agvPositionStatus.CreatedDate.AddSeconds(-MaxSilenceInSeconds))
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