using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Services;

/// <summary>
/// Handles <see cref="AgvPositionChanged"/> publishing
/// </summary>
public abstract class BaseAgvPositionRegistry : IAgvPositionRegistry
{
    private readonly IBus _bus;
    private readonly IChangeDetector<AgvPositionReceived> _agvPositionReceivedChangeDetector;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="bus">The MassTransit bus</param>
    /// <param name="agvPositionReceivedChangeDetector">The change detector which tells us whether the AgvPositionReceived has changed in the storage</param>
    protected BaseAgvPositionRegistry(IBus bus, IChangeDetector<AgvPositionReceived> agvPositionReceivedChangeDetector)
    {
        _bus = bus;
        _agvPositionReceivedChangeDetector = agvPositionReceivedChangeDetector;
    }

    /// <inheritdoc />
    public void UpdatePosition(AgvPositionReceived agvPositionReceived)
    {
        var correlationId = agvPositionReceived.CorrelationId;
        var previousAgvPosition = GetPosition(correlationId);

        UpdateInternal(agvPositionReceived);

        if (_agvPositionReceivedChangeDetector.HasChanged(previousAgvPosition, agvPositionReceived))
        {
            var agvPositionChanged = new AgvPositionChanged(agvPositionReceived.AgvPosition, previousAgvPosition?.AgvPosition, agvPositionReceived.AgvId, correlationId);
            _bus.Publish(agvPositionChanged);
        }
    }

    /// <summary>
    /// The implementation that updates the stored status
    /// </summary>
    /// <param name="agvPositionReceived">The new agv position to</param>
    protected abstract void UpdateInternal(AgvPositionReceived agvPositionReceived);

    /// <inheritdoc />
    public abstract AgvPositionReceived? GetPosition(Guid correlationId);

    /// <inheritdoc />
    public abstract List<AgvPositionReceived> GetAllPositions();
}