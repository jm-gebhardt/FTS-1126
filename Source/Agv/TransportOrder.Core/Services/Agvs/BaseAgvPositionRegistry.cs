using Gebhardt.StoreWare.Agv.Common.Messages.AgvManagement.Events;
using Gebhardt.StoreWare.Agv.Common.Services.Detectors;
using MassTransit;
using AgvPositionStatus = Gebhardt.StoreWare.Agv.TransportOrder.Core.Models.AgvPositionStatus;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Core.Services.Agvs;

/// <summary>
/// Handles <see cref="AgvPositionChanged"/> publishing
/// </summary>
public abstract class BaseAgvPositionRegistry : IAgvPositionRegistry
{
    private readonly IBus _bus;
    private readonly IChangeDetector<AgvPositionStatus> _agvPositionChangeDetector;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="bus">The MassTransit bus</param>
    /// <param name="agvPositionReceivedChangeDetector">The change detector which tells us whether the AgvPositionReceived has changed in the storage</param>
    protected BaseAgvPositionRegistry(IBus bus, IChangeDetector<AgvPositionStatus> agvPositionReceivedChangeDetector)
    {
        _bus = bus;
        _agvPositionChangeDetector = agvPositionReceivedChangeDetector;
    }

    /// <inheritdoc />
    public void UpdatePosition(AgvPositionStatus agvPositionStatus)
    {
        var correlationId = agvPositionStatus.CorrelationId;
        var previousAgvPosition = GetPosition(correlationId);

        UpdateInternal(agvPositionStatus);

        if (_agvPositionChangeDetector.HasChanged(previousAgvPosition, agvPositionStatus))
        {
            var agvPositionChanged = new AgvPositionChanged(agvPositionStatus.AgvPosition, previousAgvPosition?.AgvPosition, agvPositionStatus.AgvId, correlationId);
            _bus.Publish(agvPositionChanged);
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