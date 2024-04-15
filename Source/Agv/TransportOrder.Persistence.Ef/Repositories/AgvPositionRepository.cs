using Gebhardt.StoreWare.Agv.Common.Dictionaries;
using Gebhardt.StoreWare.Agv.Common.Domain.Models;
using Gebhardt.StoreWare.Agv.Common.Services.Filters;
using Gebhardt.StoreWare.Agv.TransportOrder.Domain.Models.Agv;
using MassTransit;

namespace Gebhardt.StoreWare.Agv.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Repository of AGV positions (in memory).
/// </summary>
public class AgvPositionRepository : BaseAgvPositionRepository
{
    private readonly IConcurrentDictionaryProvider<Guid, AgvPositionStatus> _positionDictionaryProvider;

    /// <inheritdoc/>
    public AgvPositionRepository(IBus bus, IConcurrentDictionaryProvider<Guid, AgvPositionStatus> dictionaryProvider, ISignificanceChecker<AgvPosition?> significanceChecker) : base(bus, significanceChecker)
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

    /// <inheritdoc />
    public override ICollection<AgvPositionStatus> GetSelectedAgvsPositions(ICollection<Guid> correlationIds)
    {
        return _positionDictionaryProvider.ConcurrentDict.Values.Where(v => correlationIds.Contains(v.CorrelationId)).ToList();
    }
}