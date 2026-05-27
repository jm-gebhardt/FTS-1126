using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Domain.Repositories;
using Gebhardt.Acc.TransportOrder.Messages.Events.Agv;
using MassTransit;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Handles <see cref="AgvPositionChanged"/> publishing
/// </summary>
public abstract class BaseAgvPositionRepository : IAgvPositionRepository
{
    private const int MaxSilenceInSeconds = 30;
    private readonly IBus _bus;
    private readonly ISignificanceChecker<AgvPosition> _significanceChecker;

    /// <summary>
    /// Constructor
    /// </summary>
    protected BaseAgvPositionRepository(IBus bus, ISignificanceChecker<AgvPosition> significanceChecker)
    {
        _bus = bus;
        _significanceChecker = significanceChecker;
    }

    /// <inheritdoc />
    public async Task UpdatePositionAsync(AgvPositionStatus agvPositionStatus)
    {
        var correlationId = agvPositionStatus.CorrelationId;
        var previousAgvPosition = GetPositionIfExisting(correlationId);

        if (previousAgvPosition == null ||
            agvPositionStatus.ForcePublish ||
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
    public abstract AgvPositionStatus? GetPositionIfExisting(Guid correlationId);

    /// <inheritdoc />
    public abstract AgvPositionStatus GetPosition(Guid correlationId);

    /// <inheritdoc />
    public abstract ICollection<AgvPositionStatus> GetAllPositions();

    /// <inheritdoc />
    public abstract ICollection<AgvPositionStatus> GetSelectedAgvsPositions(ICollection<Guid> correlationIds);

    /// <inheritdoc />
    public virtual Task RestorePersistedPositionsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task PersistPositionAsync(Guid correlationId, AgvPosition position, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task ClearPersistedPositionAsync(Guid correlationId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task PersistAllPositionsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}