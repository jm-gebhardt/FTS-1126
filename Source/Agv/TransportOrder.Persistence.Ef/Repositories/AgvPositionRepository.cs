using Gebhardt.Acc.Common.Dictionaries;
using Gebhardt.Acc.Common.Domain.Models;
using Gebhardt.Acc.Common.Exceptions;
using Gebhardt.Acc.Common.Services.Filters;
using Gebhardt.Acc.TransportOrder.Domain.Models.Agv;
using Gebhardt.Acc.TransportOrder.Persistence.Ef.DataAccess;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Gebhardt.Acc.TransportOrder.Persistence.Ef.Repositories;

/// <summary>
/// Repository of AGV positions (in memory).
/// </summary>
public class AgvPositionRepository : BaseAgvPositionRepository
{
    private readonly IConcurrentDictionaryProvider<Guid, AgvPositionStatus> _positionDictionaryProvider;
    private readonly IDbContextFactory<TransportOrderDbContext> _dbContextFactory;

    /// <inheritdoc/>
    public AgvPositionRepository(IBus bus, IConcurrentDictionaryProvider<Guid, AgvPositionStatus> dictionaryProvider, ISignificanceChecker<AgvPosition?> significanceChecker, IDbContextFactory<TransportOrderDbContext> dbContextFactory) : base(bus, significanceChecker)
    {
        _positionDictionaryProvider = dictionaryProvider;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    protected override void UpdateInternal(AgvPositionStatus agvPositionStatus)
    {
        var correlationId = agvPositionStatus.CorrelationId;
        _positionDictionaryProvider.ConcurrentDict.AddOrUpdate(correlationId, agvPositionStatus, (_, _) => agvPositionStatus);
    }

    /// <inheritdoc />
    public override AgvPositionStatus? GetPositionIfExisting(Guid correlationId)
    {
        _positionDictionaryProvider.ConcurrentDict.TryGetValue(correlationId, out var agvPositionReceived);
        return agvPositionReceived;
    }

    /// <inheritdoc />
    public override AgvPositionStatus GetPosition(Guid correlationId)
    {
        var agvPositionStatus = GetPositionIfExisting(correlationId);
        if (agvPositionStatus == null)
        {
            throw new AgvLocationUnknownException(correlationId);
        }
        return agvPositionStatus;
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

    /// <inheritdoc />
    public override async Task RestorePersistedPositionsAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var agvs = await dbContext.Set<Agv>()
            .Include(a => a.AgvStatus)
            .Where(a => a.AgvStatus != null && a.AgvStatus.LastKnownPosition != null)
            .ToListAsync(cancellationToken);

        foreach (var agv in agvs)
        {
            if (agv.AgvStatus?.LastKnownPosition == null)
            {
                continue;
            }

            var positionStatus = new AgvPositionStatus(agv.CorrelationId, agv.SerialNumber, agv.AgvStatus.LastKnownPosition);
            _positionDictionaryProvider.ConcurrentDict.TryAdd(agv.CorrelationId, positionStatus);
            agv.AgvStatus.UpdateLastKnownPosition(null);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task PersistPositionAsync(Guid correlationId, AgvPosition position, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var agv = await dbContext.Set<Agv>()
            .Include(a => a.AgvStatus)
            .FirstOrDefaultAsync(a => a.CorrelationId == correlationId && a.AgvStatus != null, cancellationToken);

        if (agv == null)
        {
            return;
        }

        agv.AgvStatus!.UpdateLastKnownPosition(position);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task ClearPersistedPositionAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var agv = await dbContext.Set<Agv>()
            .Include(a => a.AgvStatus)
            .FirstOrDefaultAsync(a => a.CorrelationId == correlationId && a.AgvStatus != null && a.AgvStatus.LastKnownPosition != null, cancellationToken);

        if (agv == null)
        {
            return;
        }

        agv.AgvStatus!.UpdateLastKnownPosition(null);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task PersistAllPositionsAsync(CancellationToken cancellationToken = default)
    {
        var positionsByCorrelationId = _positionDictionaryProvider.ConcurrentDict.Values.ToDictionary(p => p.CorrelationId);
        if (positionsByCorrelationId.Count == 0)
        {
            return;
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var correlationIds = positionsByCorrelationId.Keys.ToList();
        var agvs = await dbContext.Set<Agv>()
            .Include(a => a.AgvStatus)
            .Where(a => correlationIds.Contains(a.CorrelationId) && a.AgvStatus != null)
            .ToListAsync(cancellationToken);

        foreach (var agv in agvs)
        {
            if (positionsByCorrelationId.TryGetValue(agv.CorrelationId, out var position))
            {
                agv.AgvStatus!.UpdateLastKnownPosition(position.AgvPosition);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}