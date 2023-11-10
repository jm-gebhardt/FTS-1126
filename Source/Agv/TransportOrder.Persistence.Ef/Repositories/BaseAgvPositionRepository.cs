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
    private readonly IBus _bus;

    /// <summary>
    /// Constructor
    /// </summary>
    protected BaseAgvPositionRepository(IBus bus)
    {
        _bus = bus;
    }

    /// <inheritdoc />
    public async Task UpdatePositionAsync(AgvPositionStatus agvPositionStatus)
    {
        var correlationId = agvPositionStatus.CorrelationId;
        var previousAgvPosition = GetPosition(correlationId);

        UpdateInternal(agvPositionStatus);

        if (!Equals(previousAgvPosition, agvPositionStatus))
        {
            var agvPositionChanged = new AgvPositionChanged(correlationId, agvPositionStatus.AgvPosition, previousAgvPosition?.AgvPosition, agvPositionStatus.AgvId);
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