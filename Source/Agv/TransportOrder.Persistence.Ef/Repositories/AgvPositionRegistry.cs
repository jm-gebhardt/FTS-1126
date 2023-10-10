using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.SemaphoreProvider;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Registry of AGV positions (in memory)
/// </summary>
public class AgvPositionRegistry : BaseAgvPositionRegistry
{
    private readonly IConcurrentDictionaryProvider<Guid, AgvPositionStatus> _positionDictionaryProvider;

    /// <inheritdoc/>
    public AgvPositionRegistry(IBus bus, IConcurrentDictionaryProvider<Guid, AgvPositionStatus> dictionaryProvider) : base(bus)
    {
        _positionDictionaryProvider = dictionaryProvider;
    }

    /// <inheritdoc />
    protected override void UpdateInternal(AgvPositionStatus agvPositionStatus)
    {
        var correlationId = agvPositionStatus.CorrelationId;
        _positionDictionaryProvider.ConcurrentDict.AddOrUpdate(correlationId, agvPositionStatus, (_, _) => agvPositionStatus);
    }

    /// <inheritdoc />
    public override AgvPositionStatus? GetPosition(Guid correlationId)
    {
        _positionDictionaryProvider.ConcurrentDict.TryGetValue(correlationId, out var agvPositionReceived);
        return agvPositionReceived;
    }

    /// <inheritdoc />
    public override List<AgvPositionStatus> GetAllPositions()
    {
        return _positionDictionaryProvider.ConcurrentDict.Values.ToList();
    }
}
