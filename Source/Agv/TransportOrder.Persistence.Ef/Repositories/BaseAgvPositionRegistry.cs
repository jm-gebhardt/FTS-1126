using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Repositories;
using Gebhardt.StoreWare.Agv.TransportOrder.Messages.Events.Agv;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Handles <see cref="AgvPositionChanged"/> publishing
/// </summary>
public abstract class BaseAgvPositionRegistry : IAgvPositionRegistry
{
    private readonly IBus _bus;

    /// <summary>
    /// Constructor
    /// </summary>
    protected BaseAgvPositionRegistry(IBus bus)
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
            var agvPositionChanged = new AgvPositionChanged(agvPositionStatus.AgvPosition, previousAgvPosition?.AgvPosition, agvPositionStatus.AgvId, correlationId);
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
    public abstract List<AgvPositionStatus> GetAllPositions();
}